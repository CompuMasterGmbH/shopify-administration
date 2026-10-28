# Shopify-Administration

Windows-Desktop-App zur Shopify-Administration. Entwicklungsbasis ist dieses Repository; aktuelle Oberfläche: WinForms auf .NET 10.

## Anmeldung

- Vollständige HTTPS-API-Basis-URL eingeben, beispielsweise `https://shop.myshopify.com/admin/api/2023-01/`. Shop und API-Version sind konfigurierbar; eine konkrete Betriebsinstanz ist nicht im Programm hinterlegt.
- Für die bestehende Basic-Authentifizierung API-Schlüssel und Passwort/App-Token eingeben. Bei tokenbasierter Authentifizierung ohne Basic den API-Schlüssel leer lassen.
- Die Anmeldung wird durch einen lesenden Abruf der Shop-Informationen geprüft. Ungültige Zugangsdaten, Netzwerkfehler und abgelehnte Berechtigungen öffnen keine Sitzung.
- **Speichern ist standardmäßig aus**, auch bei vorausgefülltem Dialog. Ohne Häkchen bleiben die Werte nur für diese Programmsitzung im Arbeitsspeicher. Nach erfolgreicher Anmeldung ohne Häkchen wird ein früher gespeichertes Profil entfernt. Abbrechen ändert den Speicher nicht.
- Unter Windows speichert ein Häkchen die gesamte Verbindung (URL, API-Schlüssel, Passwort) mit DPAPI/CurrentUser verschlüsselt unter `%LOCALAPPDATA%\CompuMaster\ShopifyAdministration\credentials.v1.bin`. Die Datei gehört nicht in Repository, Cloud-Sync oder Releases. Sie ist an das Windows-Benutzerprofil gebunden; sie schützt nicht vor Schadsoftware, die bereits unter demselben Benutzer läuft.
- Ein vorhandenes gespeichertes Profil wird beim nächsten Start geladen und beim Server geprüft. Bei Fehlern erscheint das Anmeldeformular. Ein beschädigter Store wird nicht stillschweigend überschrieben.
- **Datei → Abmelden / Konto wechseln** beendet die Sitzung, bricht laufende Abrufe ab, schließt API-Fenster und leert angezeigte Daten. Die letzte Verbindung ist im Anmeldeformular vorausgefüllt. Dort sind andere URLs/Konten oder eine erneute Anmeldung möglich. Die Werte bleiben beim Abmelden zunächst im RAM; ein gespeichertes Profil bleibt bis zur erfolgreichen Anmeldung ohne Häkchen erhalten.
- Das Passwort ist zunächst maskiert und per **Anzeigen / Verbergen** umschaltbar.
- Anfragen einschließlich Pagination dürfen die konfigurierte HTTPS-Shop-API nicht verlassen. HTTP-Redirects werden nicht automatisch verfolgt.

## Aufbau und Plattformen

- `ShopifyAdministration.Core`: UI-/plattformunabhängige Verbindung, Anmeldung, Sitzungslebenszyklus, HTTP-Transport und Speichervertrag.
- `ShopifyAdministration.Platform`: Windows-DPAPI-Adapter mit expliziter Betriebssystemprüfung und Factory.
- `SessionOnlyCredentialStore`: Fallback für Plattformen ohne sicheren Adapter; Speichern ist nicht möglich, Anmeldung ohne Speicherung bleibt möglich.
- `Shopify-Administration`: derzeit Windows-spezifische WinForms-Oberfläche und Datenadapter. Die bestehende Oberfläche selbst läuft nicht auf Linux/macOS.
- [UI-001: Avalonia-/WinUI-Migration](docs/tickets/UI-001-framework-migration.md) enthält Rahmen, Plattformadapter und Abnahmekriterien.

## Build und Tests

.NET 10 SDK und Windows für die Oberfläche vorausgesetzt:

```powershell
dotnet build Shopify-Administration.sln -c Release
dotnet run --project ShopifyAdministration.Tests -c Release
dotnet run --project ShopifyAdministration.UiTests -c Release
```

Die Core-/Speichertests nutzen Testzugangsdaten und einen Fake-HTTP-Handler; Windows prüft zusätzlich echte DPAPI-Verschlüsselung in einem temporären Verzeichnis. Die UI-Tests prüfen automatischen Login, Abmeldung, Vorausfüllen, Konto-Wechsel, Datenbereinigung, Passwortanzeige und standardmäßig ausgeschaltete Speicherung mit Fake-Server und Fake-Store. Sie senden keine Betriebscredentials an Shopify.

## Lizenz und Veröffentlichung

Der eigene Quellcode steht unter der [MIT-Lizenz](LICENSE). Die Lizenzen der verwendeten Bibliotheken gelten separat; siehe [Drittanbieterhinweise](THIRD_PARTY_NOTICES.md), insbesondere für EPPlus 4.5.3.2 unter LGPL.

Das öffentliche Repository beginnt mit einem bereinigten Initial-Commit ohne die frühere private Git-Historie. Es enthält keine Betriebszugangsdaten, lokalen Anmeldespeicher oder Build-Ausgaben. Ein eigener Shopify-Zugang wird ausschließlich lokal im Anmeldeformular konfiguriert.
