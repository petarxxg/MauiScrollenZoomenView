# MAUI ZoomPanCanvas - Zoom & Pan View für .NET MAUI

Eine produktionsreife, vollständig funktionale Zoom & Pan Canvas-Komponente für .NET MAUI Apps (Android, iOS, Windows).

## 🎯 Überblick

Diese Komponente ermöglicht es, **beliebige Inhalte in Ihrer bestehenden MAUI App** mit Zoom- und Pan-Funktionalität auszustatten. Perfekt für:
- Tischpläne / Sitzpläne
- Lagepläne / Grundrisse
- Interaktive Karten
- Bildergalerien mit Zoom
- Technische Zeichnungen
- Jede Art von skalierbarer Ansicht

## ✨ Features

- ✅ **Touch-Gestures**: Pinch-to-Zoom und Pan auf mobilen Geräten
- ✅ **Mausrad-Zoom**: Volle Desktop-Unterstützung (Windows) mit Mausrad
- ✅ **Zoom-Fokus auf Cursor**: Zoom zentriert sich auf Maus-/Touch-Position
- ✅ **Konfigurierbare Grenzen**: Min/Max Zoom-Levels einstellbar
- ✅ **Freies Panning**: Scrollen über sichtbare Grenzen hinaus
- ✅ **Keine Zurücksprünge**: Position bleibt nach Gesteneende erhalten
- ✅ **Hit-Testing funktioniert**: Tap-Gesten auf Elemente auch bei Zoom/Pan
- ✅ **Reine MAUI**: Keine externen Dependencies (SkiaSharp, etc.)
- ✅ **Cross-Platform**: Android, iOS, Windows

## 🚀 Integration in Ihre bestehende App

### Schritt 1: Dateien kopieren

Kopieren Sie folgende Dateien in Ihr bestehendes MAUI-Projekt:

```
YourApp/
├── Controls/
│   └── ZoomPanCanvas.cs          ← Diese Datei kopieren
└── Models/
    └── TableModel.cs             ← Optional: Nur für Beispiel-Daten
```

**Wichtig**: Passen Sie die Namespaces in den kopierten Dateien an Ihre App an:

```csharp
// In ZoomPanCanvas.cs
namespace IhreApp.Controls;  // ← Ihr Namespace

// In TableModel.cs
namespace IhreApp.Models;    // ← Ihr Namespace
```

### Schritt 2: XAML-Integration

Ersetzen Sie Ihre bestehende View durch die ZoomPanCanvas:

**Vorher (Ihre alte View):**
```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             x:Class="IhreApp.TischplanPage">

    <AbsoluteLayout x:Name="TischplanLayout">
        <!-- Ihre Tische hier -->
    </AbsoluteLayout>

</ContentPage>
```

**Nachher (Mit ZoomPanCanvas):**
```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:controls="clr-namespace:IhreApp.Controls"
             x:Class="IhreApp.TischplanPage">

    <!-- Ihre bestehenden Inhalte kommen IN die ZoomPanCanvas -->
    <controls:ZoomPanCanvas x:Name="ZoomPanCanvas" />

</ContentPage>
```

### Schritt 3: Code-Behind anpassen

**Variante A: Sie haben bereits Tisch-Daten (z.B. aus Datenbank)**

```csharp
using IhreApp.Controls;
using IhreApp.Models;

namespace IhreApp;

public partial class TischplanPage : ContentPage
{
    public TischplanPage()
    {
        InitializeComponent();
        LoadIhreTische();
    }

    private void LoadIhreTische()
    {
        // Ihre bestehenden Tisch-Daten laden
        var tische = await DatenbankService.HoleTische();

        // In TableModel-Format konvertieren
        var tableModels = tische.Select(t => new TableModel
        {
            Id = t.Id,
            Name = t.Name,
            X = t.XPosition,
            Y = t.YPosition,
            Width = t.Breite,
            Height = t.Hoehe
        }).ToList();

        // In ZoomPanCanvas laden
        ZoomPanCanvas.LoadTables(tableModels);
    }
}
```

**Variante B: Sie haben bereits visuelle Elemente (Views)**

Wenn Sie schon `ContentView`-Elemente für Ihre Tische haben:

```csharp
public void LoadIhreExistierendenViews()
{
    // Zugriff auf das interne AbsoluteLayout der ZoomPanCanvas
    var canvas = ZoomPanCanvas.GetCanvas(); // ← Methode hinzufügen (siehe unten)

    foreach (var tisch in IhreBestehendenTischViews)
    {
        AbsoluteLayout.SetLayoutBounds(tisch, new Rect(x, y, width, height));
        canvas.Children.Add(tisch);
    }
}
```

**Dafür müssen Sie in `ZoomPanCanvas.cs` eine öffentliche Methode hinzufügen:**

```csharp
// In ZoomPanCanvas.cs
public AbsoluteLayout GetCanvas()
{
    return _canvas;
}
```

### Schritt 4: Canvas-Größe anpassen

Die Canvas-Größe sollte Ihren tatsächlichen Koordinaten entsprechen. Passen Sie in `ZoomPanCanvas.cs` an:

```csharp
public ZoomPanCanvas()
{
    _canvas = new AbsoluteLayout
    {
        WidthRequest = 3000,   // ← Ihre Canvas-Breite
        HeightRequest = 2000,  // ← Ihre Canvas-Höhe
        BackgroundColor = Colors.White
    };
    // ...
}
```

**Wie finde ich die richtige Größe?**
- Nehmen Sie die maximalen X/Y-Koordinaten Ihrer Elemente + Puffer
- Beispiel: Größter X-Wert = 2500, größter Y-Wert = 1800 → Canvas = 3000×2000

### Schritt 5: Eigene Tap-Handler behalten

Wenn Ihre Tische bereits Tap-Gesten haben, funktionieren diese weiterhin:

```csharp
var meinTisch = new ContentView
{
    Content = new Border { /* ... */ }
};

// Ihre bestehenden Tap-Handler bleiben erhalten
var tapGesture = new TapGestureRecognizer();
tapGesture.Tapped += async (s, e) =>
{
    // Ihr Code hier - funktioniert trotz Zoom/Pan!
    await IhreTischDetailsAnzeigen(tischId);
};
meinTisch.GestureRecognizers.Add(tapGesture);
```

## 🔧 Konfiguration

### Zoom-Grenzen ändern

```csharp
// In ZoomPanCanvas.cs
private const double MinScale = 0.5;   // Minimaler Zoom (50%)
private const double MaxScale = 3.0;   // Maximaler Zoom (300%)
```

### Zoom-Geschwindigkeit (Mausrad) anpassen

```csharp
// In ZoomPanCanvas.cs, OnPointerWheelChanged
var zoomFactor = delta > 0 ? 1.1 : 0.9;  // 10% pro Tick
// Größerer Wert = schnelleres Zoomen
var zoomFactor = delta > 0 ? 1.2 : 0.8;  // 20% pro Tick
```

### Hintergrundfarbe ändern

```csharp
// Canvas-Hintergrund
_canvas = new AbsoluteLayout
{
    BackgroundColor = Colors.LightGray  // Ihre Farbe
};

// Äußerer Bereich (außerhalb Canvas)
_rootGrid = new Grid
{
    BackgroundColor = Colors.DarkGray   // Ihre Farbe
};
```

## 📱 Platform-spezifisches Verhalten

### Android & iOS
- ✅ Pinch-to-Zoom: Zwei Finger zusammen/auseinander
- ✅ Pan: Ein Finger wischen
- ✅ Tap: Einmal tippen

### Windows (Desktop)
- ✅ Zoom: Mausrad hoch/runter
- ✅ Pan: Linke Maustaste gedrückt halten + Ziehen
- ✅ Tap: Linksklick
- ✅ Touchpad: Pinch-Geste funktioniert auch

## 🎨 Anpassung der Tisch-Darstellung

Die Standard-Tischdarstellung können Sie in `CreateTableView()` anpassen:

```csharp
private ContentView CreateTableView(TableModel table)
{
    var border = new Border
    {
        Stroke = Colors.DarkBlue,      // Rahmenfarbe
        StrokeThickness = 2,            // Rahmendicke
        BackgroundColor = Colors.LightBlue,  // Füllfarbe
        StrokeShape = new RoundRectangle { CornerRadius = 8 },
        Content = new Label
        {
            Text = table.Name,
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.DarkBlue
        }
    };

    // Hier Ihr eigenes Design einfügen
    // z.B. Icons, mehrere Labels, Bilder, etc.

    return new ContentView { Content = border };
}
```

## 🔄 Migration von bestehenden Layouts

### Von ScrollView mit AbsoluteLayout

**Vorher:**
```xml
<ScrollView>
    <AbsoluteLayout x:Name="MeinLayout">
        <!-- Elemente -->
    </AbsoluteLayout>
</ScrollView>
```

**Nachher:**
```xml
<controls:ZoomPanCanvas x:Name="ZoomPanCanvas" />
```

Dann im Code:
```csharp
var canvas = ZoomPanCanvas.GetCanvas();
// Alle Ihre Elemente zu canvas.Children hinzufügen
```

### Von Grid/StackLayout

Wenn Ihre Elemente relative Positionen haben, müssen Sie sie in absolute Positionen konvertieren:

```csharp
// Beispiel: Gleichmäßig verteilt
for (int i = 0; i < tische.Count; i++)
{
    double x = 100 + (i % 5) * 250;  // 5 pro Reihe, 250px Abstand
    double y = 100 + (i / 5) * 150;  // 150px zwischen Reihen

    AbsoluteLayout.SetLayoutBounds(tische[i], new Rect(x, y, 120, 80));
}
```

## 🐛 Troubleshooting

### Problem: Elemente sind nicht sichtbar

**Lösung**: Canvas-Größe überprüfen
```csharp
// Canvas muss groß genug für alle Elemente sein
_canvas.WidthRequest = MaxX + 500;   // Max X-Koordinate + Puffer
_canvas.HeightRequest = MaxY + 500;  // Max Y-Koordinate + Puffer
```

### Problem: Zoom funktioniert nicht

**Lösung Windows**: Stellen Sie sicher, dass die Handler-Changed-Methode läuft:
```csharp
// In ZoomPanCanvas.cs
this.HandlerChanged += OnHandlerChanged;
```

### Problem: Tap-Gesten reagieren nicht

**Lösung**: InputTransparent auf false setzen:
```csharp
var element = new ContentView
{
    InputTransparent = false  // ← Wichtig!
};
```

### Problem: Pan ist ruckelig

**Lösung**: Reduzieren Sie die Anzahl der Kinder oder nutzen Sie Virtualisierung für große Datenmengen.

## 📋 Vollständiges Beispiel

Siehe `src/TischplanApp/` für ein vollständiges, funktionierendes Beispiel mit:
- 12 Beispiel-Tischen
- Verschiedenen Größen und Positionen
- Tap-Handlern auf jedem Tisch
- Windows + Android + iOS Support

### Beispiel starten:

**Visual Studio:**
1. Öffne `TischplanApp.sln`
2. Wähle Platform (Windows/Android/iOS)
3. F5 drücken

**CLI:**
```bash
# Windows
dotnet build src/TischplanApp/TischplanApp.csproj -t:Run -f net9.0-windows10.0.19041.0

# Android
dotnet build src/TischplanApp/TischplanApp.csproj -t:Run -f net9.0-android

# iOS (nur macOS)
dotnet build src/TischplanApp/TischplanApp.csproj -t:Run -f net9.0-ios
```

## 🛠️ Technische Details

- **Framework**: .NET 9 MAUI
- **MAUI Version**: 9.0.10
- **Target Platforms**: Android, iOS, Windows
- **Minimum Android**: API 21 (Android 5.0)
- **Minimum iOS**: 14.2
- **Minimum Windows**: Windows 10 Build 19041

## 📄 Lizenz

Dieses Projekt ist Open Source und für kommerzielle und private Projekte frei nutzbar.

## 🤝 Beiträge

Verbesserungen und Bug-Fixes sind willkommen! Erstellen Sie einfach einen Pull Request.

## 💡 Tipps & Best Practices

### Performance

- **Lazy Loading**: Laden Sie nur sichtbare Elemente bei sehr großen Datasets
- **Virtualisierung**: Für 1000+ Elemente erwägen Sie Virtualisierung
- **Bild-Optimierung**: Nutzen Sie komprimierte Bilder für Hintergründe

### UX

- **Visuelle Grenzen**: Zeigen Sie Nutzern, wo die Canvas-Grenzen sind
- **Zoom-Level-Anzeige**: Optional eine Zoom-Prozentzahl anzeigen
- **Reset-Button**: Bieten Sie einen Button zum Zurücksetzen der Ansicht

### Erweiterungen

- **Mehrere Canvas-Größen**: Passen Sie dynamisch an Bildschirmgröße an
- **Rotation**: Erweitern Sie um Rotations-Support
- **Snap-to-Grid**: Fügen Sie Raster-Snapping hinzu
- **Kollisionserkennung**: Prüfen Sie Überlappungen von Elementen

---

**Viel Erfolg bei der Integration in Ihre App!** 🎉

Bei Fragen: Issue erstellen oder die Demo-App als Referenz nutzen.
