# UI-001: Oberfläche auf Avalonia oder WinUI umstellen

Status: Offen
Erstellt: 28.10.2026
Priorität: Geplante technische Weiterentwicklung
Ausgangslage: WinForms-Desktop-App, seit dieser Änderung auf .NET 10.

## Ziel

Frameworkentscheidung und Umsetzung vorbereiten, ohne Anmeldedaten, Sitzung und API-Zugriff erneut an eine Oberfläche zu koppeln. Avalonia ist für eine plattformübergreifende Oberfläche zu prüfen; WinUI ist eine Windows-Option. Die Auswahl ist noch offen.

## Bereits umgesetzt

- ShopifyAdministration.Core (netstandard2.0) enthält Verbindungsvalidierung, API-Transport, Anmeldung/Abmeldung und ICredentialStore; keine WinForms-/Windows-Abhängigkeit.
- ShopifyAdministration.Platform (net10.0) kapselt den Windows-DPAPI-Speicher. Die Factory wählt ihn nur unter Windows.
- SessionOnlyCredentialStore ist der sichere Fallback: Anmeldung ist möglich, dauerhafte Speicherung ist nicht verfügbar. Es gibt keinen Klartext-Fallback.
- Der aktuelle WinForms-Dialog ist ein austauschbarer Adapter. Speicherung ist standardmäßig aus; Passwortanzeige bleibt eine bewusste Benutzeraktion.
- Windows-.NET-10-UI und Bibliotheken sowie Tests werden getrennt gebaut.

## Aufgaben und Abnahmekriterien

1. Entscheidung Avalonia vs. WinUI dokumentieren: Zielplattformen, Deployment, Barrierefreiheit, DPI, Wartung und Lizenz.
2. Anmeldeansicht/Status/Fehler als UI-unabhängiges ViewModel oder Presenter ergänzen; Core-API unverändert verwenden.
3. Windows-spezifische Dateien/Shell-Aktionen/Export-Dialoge in eigene UI-/Plattformadapter verschieben; gemeinsam genutzten Code auf direkte OS-Aufrufe prüfen.
4. Für Avalonia auf macOS einen Keychain-Adapter, auf Linux einen Secret-Service-Adapter prüfen. Fehlender/gesperrter Secret Store darf niemals zu Klartextdateien führen.
5. Auf jeder Plattform prüfen: erstmalig nicht speichern; explizit speichern; automatischer Login erst nach Servervalidierung; Abmelden beendet Anfragen, leert alte Shop-Daten und öffnet vorausgefüllte Anmeldung; Konto-/URL-Wechsel; Passwort anzeigen/verbergen; Speichern wieder abwählen; beschädigter/gesperrter Store; Abbrechen/Timeout.
6. Native Verschlüsselungs-APIs ausschließlich im jeweiligen Plattformadapter und mit Plattformprüfung ausführen. Fallback muss ohne diese APIs starten.
7. Automatisierte Core-Tests unter Windows/Linux/macOS und UI-/Store-Integrationstests je unterstützter Plattform ausführen.
8. .NET 10 und unterstützte API-/Paketversionen für die Zieloberflächen festlegen; vorhandene Alt-GraphQL-/ShopifySharp-Adapter bei der funktionalen Migration konsolidieren.

## Tracker

Server-Ticket: [GitHub #1](https://github.com/CompuMasterGmbH/shopify-administration/issues/1). Diese Datei hält den Umsetzungskontext zusätzlich beim Quellcode fest.
