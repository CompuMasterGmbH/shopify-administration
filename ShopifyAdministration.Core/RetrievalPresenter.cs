using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ShopifyAdministration.Core
{
    /// <summary>Owns atomic dataset loading and prevents results from crossing login sessions.</summary>
    public sealed class RetrievalPresenter : INotifyPropertyChanged, IDisposable
    {
        private readonly LoginSession session;
        private CancellationTokenSource pending;
        private int generation;
        /// <summary>Initializes an empty view bound to the supplied session.</summary>
        public RetrievalPresenter(LoginSession session) => this.session = session ?? throw new ArgumentNullException(nameof(session));
        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>Gets the complete current records, or an empty list while loading or after failure.</summary>
        public IReadOnlyList<JObject> Records { get; private set; } = Array.Empty<JObject>();
        /// <summary>Gets whether a refresh is in progress.</summary>
        public bool IsLoading { get; private set; }
        /// <summary>Gets whether a complete successful dataset may be exported.</summary>
        public bool CanExport { get; private set; }
        /// <summary>Gets safe progress or error information without record contents.</summary>
        public string Status { get; private set; } = "Bereit";
        /// <summary>Loads a dataset asynchronously; overlapping requests are ignored.</summary>
        public async Task LoadAsync(ShopifyDataset dataset, CancellationToken cancellationToken = default)
        {
            var client = session.ActiveClient;
            if (client == null || IsLoading) return;
            var current = ++generation;
            var operation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            operation.CancelAfter(TimeSpan.FromMinutes(10));
            pending = operation;
            IsLoading = true; Records = Array.Empty<JObject>(); CanExport = false; Status = "Daten werden vollständig geladen …"; Changed();
            try
            {
                var progress = new Progress<int>(page =>
                {
                    if (current == generation && IsLoading) { Status = "Daten werden geladen – Seite " + page; Changed(); }
                });
                var records = await new ShopifyDataReader(client).ReadAsync(dataset, progress, operation.Token);
                if (current != generation || !ReferenceEquals(client, session.ActiveClient)) return;
                operation.Token.ThrowIfCancellationRequested();
                Records = records; CanExport = true; Status = records.Count + " Datensätze vollständig geladen";
            }
            catch (OperationCanceledException) { if (current == generation) Status = "Abruf abgebrochen oder Zeitüberschreitung. Keine Teildaten übernommen."; }
            catch (Exception ex)
            {
                if (current == generation) Status = ex is InvalidOperationException ? ex.Message : "Abruf fehlgeschlagen. Keine Teildaten übernommen.";
            }
            finally
            {
                operation.Dispose();
                if (current == generation) { pending = null; IsLoading = false; Changed(); }
            }
        }
        /// <summary>Cancels the current refresh, including waits, without retaining partial data.</summary>
        public void Cancel() => pending?.Cancel();
        /// <summary>Invalidates old results immediately when logging out or switching accounts.</summary>
        public void Clear()
        {
            ++generation; pending?.Cancel(); pending = null;
            IsLoading = false; CanExport = false; Records = Array.Empty<JObject>(); Status = "Nicht angemeldet"; Changed();
        }
        /// <inheritdoc />
        public void Dispose() => Clear();
        private void Changed() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }
}
