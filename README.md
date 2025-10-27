# MAUI ZoomPanCanvas - Production-Ready Zoom & Pan für .NET MAUI

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![MAUI](https://img.shields.io/badge/MAUI-10.0.0--preview-512BD4)](https://dotnet.microsoft.com/apps/maui)
[![Platform](https://img.shields.io/badge/Platform-Android%20%7C%20iOS%20%7C%20Windows-lightgrey)](https://github.com/petarxxg/MauiScrollenZoomenView)
[![License](https://img.shields.io/badge/License-Open%20Source-green)](https://github.com/petarxxg/MauiScrollenZoomenView)

Eine **produktionsreife, buttery-smooth** Zoom & Pan Canvas-Komponente für .NET MAUI Apps mit **nativen Gesture Handlers** für Android und iOS.

> **⚡ .NET 10 Ready!** Dieses Projekt wurde auf .NET 10 migriert und ist vollständig kompatibel mit Visual Studio 2026 Insiders.

## 🎯 Überblick

Diese Komponente ermöglicht es, **beliebige Inhalte in Ihrer bestehenden MAUI App** mit Google-Maps-ähnlicher Zoom- und Pan-Funktionalität auszustatten. Perfekt für:
- 🪑 Tischpläne / Sitzpläne / Raumpläne
- 🗺️ Lagepläne / Grundrisse / Karten
- 🖼️ Bildergalerien mit Zoom
- 📐 Technische Zeichnungen / CAD-Ansichten
- 📊 Jede Art von skalierbarer Ansicht

## 🎮 Bedienung (Quick Start)

### 🖥️ Am PC (Windows)
- **Reinzoomen**: Mausrad nach oben drehen ↑
- **Rauszoomen**: Mausrad nach unten drehen ↓
- **Verschieben**: Linke Maustaste gedrückt halten + Maus bewegen

### 🎮 Android Emulator (Desktop-Testing)
- **Zoom Methode 1**: Mausrad nach oben/unten ↑↓ (einfachste Methode)
- **Zoom Methode 2**: Strg + Linke Maustaste + Ziehen (Pinch-Simulation)
- **Verschieben**: Linke Maustaste (ohne Strg) + Ziehen

### 📱 Am Smartphone/Tablet (Android/iOS)
- **Zoomen**: Zwei Finger zusammen/auseinander bewegen (Pinch)
- **Verschieben**: Ein Finger wischen

## ✨ Features

- ✅ **Native Gesture Recognizers**: Direkte Nutzung von Android ScaleGestureDetector und iOS UIPinchGestureRecognizer
- ✅ **Buttery-Smooth Performance**: Keine MAUI-Zwischenschicht, direkt Hardware-beschleunigt
- ✅ **Proportionale Skalierung**: Was du siehst wird größer/kleiner OHNE Verschiebung
- ✅ **Touch-Gestures**: Pinch-to-Zoom und Pan auf Android & iOS
- ✅ **Mausrad-Zoom**: Volle Desktop-Unterstützung (Windows)
- ✅ **Konfigurierbare Grenzen**: Min/Max Zoom-Levels einstellbar (Standard: 0.1x - 3.0x)
- ✅ **Freies Panning**: Scrollen über sichtbare Grenzen hinaus
- ✅ **Hit-Testing funktioniert**: Tap-Gesten auf Elemente auch bei Zoom/Pan
- ✅ **Reine MAUI**: Keine externen Dependencies
- ✅ **Cross-Platform**: Android, iOS, Windows
- ✅ **.NET 10 Kompatibel**: Voll migriert auf .NET 10 mit allen API-Updates

## 📦 Voraussetzungen

### Empfohlene Entwicklungsumgebung

- **Visual Studio 2026 Insiders** (empfohlen für .NET 10)
- **.NET 10 SDK** (10.0.100-rc.2 oder höher)
- **MAUI Workload** für .NET 10

### Installation .NET 10 SDK

```bash
# SDK-Version prüfen
dotnet --version

# Sollte mindestens 10.0.100-rc.2 sein
```

### MAUI Workload installieren

```bash
dotnet workload install maui
```

## 🔄 .NET 10 Migration Guide

Falls Sie von .NET 9 upgraden, beachten Sie diese Breaking Changes:

### 1. App.xaml.cs - Window Initialisierung

**❌ Alt (.NET 9):**
```csharp
public App()
{
    InitializeComponent();
    MainPage = new MainPage();  // Deprecated in .NET 10
}
```

**✅ Neu (.NET 10):**
```csharp
public App()
{
    InitializeComponent();
}

protected override Window CreateWindow(IActivationState? activationState)
{
    return new Window(new MainPage());
}
```

### 2. DisplayAlert API

**❌ Alt:**
```csharp
await Application.Current.MainPage.DisplayAlert("Titel", "Text", "OK");
```

**✅ Neu:**
```csharp
var window = Application.Current?.Windows.FirstOrDefault();
await window.Page.DisplayAlertAsync("Titel", "Text", "OK");
```

### 3. iOS Handler - ContentView Namenskonflikt

In .NET 10 gibt es einen Namenskonflikt zwischen `Microsoft.Maui.Controls.ContentView` und `Microsoft.Maui.Platform.ContentView`.

**Lösung in ZoomPanCanvasHandler.cs (iOS):**
```csharp
using PlatformView = Microsoft.Maui.Platform.ContentView;

protected override PlatformView CreatePlatformView() { ... }
```

## 🚀 Integration in Ihre bestehende App

### Schritt 1: Dateien kopieren

Kopieren Sie folgende Dateien in Ihr MAUI-Projekt:

```
YourApp/
├── Controls/
│   └── ZoomPanCanvas.cs                              ← PFLICHT
├── Models/
│   └── TableModel.cs                                 ← Optional (nur für Demo)
└── Platforms/
    ├── Android/
    │   └── Handlers/
    │       └── ZoomPanCanvasHandler.cs               ← PFLICHT für Android
    └── iOS/
        └── Handlers/
            └── ZoomPanCanvasHandler.cs               ← PFLICHT für iOS
```

**Namespace anpassen:**

```csharp
// In allen kopierten Dateien:
namespace IhreApp.Controls;    // ← Ihr App-Namespace
namespace IhreApp.Models;      // ← Ihr App-Namespace
namespace IhreApp.Platforms.Android.Handlers;  // ← Ihr App-Namespace
namespace IhreApp.Platforms.iOS.Handlers;      // ← Ihr App-Namespace
```

### Schritt 2: Handler registrieren (WICHTIG!)

In `MauiProgram.cs`:

```csharp
using Microsoft.Extensions.Logging;
using IhreApp.Controls;  // ← Ihr Namespace

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => { /* ... */ })
            // ↓↓↓ DIESE ZEILEN HINZUFÜGEN ↓↓↓
            .ConfigureMauiHandlers(handlers =>
            {
#if ANDROID
                handlers.AddHandler<ZoomPanCanvas, Platforms.Android.Handlers.ZoomPanCanvasHandler>();
#elif IOS
                handlers.AddHandler<ZoomPanCanvas, Platforms.iOS.Handlers.ZoomPanCanvasHandler>();
#endif
            });
            // ↑↑↑ BIS HIER ↑↑↑

        return builder.Build();
    }
}
```

**⚠️ Ohne diese Registrierung funktioniert die native Gesture-Erkennung nicht!**

### Schritt 3: XAML-Integration

**In Ihrer Page:**

```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:controls="clr-namespace:IhreApp.Controls"
             x:Class="IhreApp.YourPage">

    <controls:ZoomPanCanvas x:Name="ZoomPanCanvas" />

</ContentPage>
```

### Schritt 4: Inhalte laden

**Variante A: Mit TableModel (Demo-Daten):**

```csharp
using IhreApp.Controls;
using IhreApp.Models;

public partial class YourPage : ContentPage
{
    public YourPage()
    {
        InitializeComponent();
        LoadTables();
    }

    private void LoadTables()
    {
        var tables = new List<TableModel>
        {
            new TableModel { Id = 1, Name = "Tisch 1", X = 300, Y = 200, Width = 140, Height = 90 },
            new TableModel { Id = 2, Name = "Tisch 2", X = 600, Y = 200, Width = 140, Height = 90 },
            new TableModel { Id = 3, Name = "Tisch 3", X = 900, Y = 200, Width = 140, Height = 90 }
        };

        ZoomPanCanvas.LoadTables(tables);
    }
}
```

**Variante B: Eigene Views hinzufügen:**

```csharp
// Zugriff auf das AbsoluteLayout im Canvas
public AbsoluteLayout GetCanvas()
{
    // Diese Methode in ZoomPanCanvas.cs hinzufügen:
    public AbsoluteLayout Canvas => _canvas;
}

// Dann in Ihrer Page:
var canvas = ZoomPanCanvas.Canvas;
var myView = new Border
{
    Stroke = Colors.Blue,
    Content = new Label { Text = "Mein Element" }
};

AbsoluteLayout.SetLayoutBounds(myView, new Rect(100, 100, 200, 100));
canvas.Children.Add(myView);
```

## ⚙️ Konfiguration

### Canvas-Größe anpassen

**Wo:** `ZoomPanCanvas.cs`, Zeile ~28-33

```csharp
_canvas = new AbsoluteLayout
{
    WidthRequest = 3000,   // ← Ihre Canvas-Breite
    HeightRequest = 2000,  // ← Ihre Canvas-Höhe
    BackgroundColor = Colors.White
};
```

**Wie berechnen?**
- Nehmen Sie die maximalen X/Y-Koordinaten Ihrer Elemente + Puffer
- Beispiel: Größter X = 2500, größter Y = 1800 → Canvas = 3000 × 2000

### Ausgangspunkt (Initiale Position & Zoom) festlegen

**Wo:** `ZoomPanCanvas.cs`, Zeile ~70-80 in `OnSizeChanged()`

**Standard:** Canvas ist zentriert, Zoom = 1.0

```csharp
private void OnSizeChanged(object? sender, EventArgs e)
{
    if (!_isInitialized && Width > 0 && Height > 0)
    {
        // ↓↓↓ HIER Ausgangspunkt ändern ↓↓↓

        // Standard: Zentriert, Zoom 1.0
        _currentScale = 1.0;
        _xOffset = 0;
        _yOffset = 0;

        // Beispiel: Initial herausgezoomt (50%)
        // _currentScale = 0.5;
        // _xOffset = 0;
        // _yOffset = 0;

        // Beispiel: Initial auf bestimmte Position verschoben
        // _currentScale = 1.0;
        // _xOffset = -200;  // 200px nach links verschoben
        // _yOffset = -100;  // 100px nach oben verschoben

        // Beispiel: Reingezoomt auf einen bestimmten Bereich
        // _currentScale = 1.5;
        // _xOffset = -300;
        // _yOffset = -200;

        _contentHost.Scale = _currentScale;
        _contentHost.TranslationX = _xOffset;
        _contentHost.TranslationY = _yOffset;
        _isInitialized = true;
    }
}
```

**Tipp:** Negative Offsets verschieben den Inhalt nach oben/links, positive nach unten/rechts.

### Zoom-Grenzen anpassen

**Wo:** `ZoomPanCanvas.cs`, Zeile ~12-13

```csharp
private const double MinScale = 0.1;   // Minimaler Zoom (10% = weit rausgezoomt)
private const double MaxScale = 3.0;   // Maximaler Zoom (300% = weit reingezoomt)
```

**Auch in den Platform Handlers anpassen:**

**Android:** `Platforms/Android/Handlers/ZoomPanCanvasHandler.cs`, Zeile ~97
```csharp
newScale = Math.Max(0.1f, Math.Min(3.0f, newScale));  // ← Hier anpassen
```

**iOS:** `Platforms/iOS/Handlers/ZoomPanCanvasHandler.cs`, Zeile ~61
```csharp
newScale = (nfloat)Math.Max(0.1, Math.Min(3.0, newScale));  // ← Hier anpassen
```

### Hintergrundfarbe ändern

**Wo:** `ZoomPanCanvas.cs`

```csharp
// Canvas-Hintergrund (Zeile ~32)
_canvas = new AbsoluteLayout
{
    BackgroundColor = Colors.White  // ← Ihre Farbe
};

// Äußerer Bereich (außerhalb Canvas, Zeile ~48)
_rootGrid = new Grid
{
    BackgroundColor = Colors.LightGray  // ← Ihre Farbe
};
```

## 🎨 Elemente-Darstellung anpassen

### Beispiel: Tisch-Darstellung ändern

**Wo:** `ZoomPanCanvas.cs`, Methode `CreateTableView()`, Zeile ~135-178

```csharp
private ContentView CreateTableView(TableModel table)
{
    // Hier Ihr eigenes Design erstellen
    var border = new Border
    {
        Stroke = Colors.DarkBlue,           // ← Rahmenfarbe
        StrokeThickness = 2,                // ← Rahmendicke
        BackgroundColor = Colors.LightBlue, // ← Füllfarbe
        StrokeShape = new RoundRectangle { CornerRadius = 8 },
        Content = new Label
        {
            Text = table.Name,
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.DarkBlue
        }
    };

    // Tap-Geste hinzufügen
    var tableView = new ContentView { Content = border };
    var tapGesture = new TapGestureRecognizer();
    tapGesture.Tapped += async (s, e) =>
    {
        // Ihr Click-Handler
        await DisplayAlert("Info", $"{table.Name} angeklickt!", "OK");
    };
    tableView.GestureRecognizers.Add(tapGesture);

    return tableView;
}
```

## 📱 Plattform-spezifisches Verhalten

### Android

**Auf Gerät / Touch:**
- ✅ **Native ScaleGestureDetector**: Direkt vom Android OS
- ✅ **Pinch-to-Zoom**: Zwei Finger zusammen/auseinander
- ✅ **Pan**: Ein Finger wischen
- ✅ **Hardware-beschleunigt**: `android:hardwareAccelerated="true"` in AndroidManifest.xml

**Im Android Emulator (Desktop-Testing):**
- ✅ **Mausrad-Zoom**: 🖱️ Mausrad nach oben/unten = Direkter Zoom in/out
- ✅ **Pinch-Zoom simulieren**: Strg + Linke Maustaste + Ziehen = Pinch-Geste simulieren
- ✅ **Maus-Pan**: Linke Maustaste (ohne Strg) + Ziehen = Verschieben
- 💡 **Tipp**: Beide Zoom-Methoden funktionieren! Mausrad ist am einfachsten.
- 💡 **Technisch**: GenericMotionEvent mit Axis.Vscroll für Mausrad-Support

### iOS
- ✅ **Native UIPinchGestureRecognizer**: Direkt vom iOS UIKit
- ✅ **Pinch-to-Zoom**: Zwei Finger zusammen/auseinander
- ✅ **Pan**: Ein Finger wischen
- ✅ **Simultaneous Gestures**: Zoom und Pan gleichzeitig möglich

### Windows (PC / Laptop)

**Mit Maus:**
- ✅ **Zoom reinzoomen**: 🖱️ **Mausrad nach oben** drehen (scrollen)
- ✅ **Zoom rauszoomen**: 🖱️ **Mausrad nach unten** drehen (scrollen)
- ✅ **Canvas verschieben (Pan)**: 🖱️ **Linke Maustaste** gedrückt halten + Maus bewegen

**Mit Touch-Display:**
- ✅ **Pinch-to-Zoom**: Zwei Finger zusammen/auseinander bewegen
- ✅ **Pan**: Ein Finger über Display wischen

**Technische Details:**
- Windows nutzt MAUI GestureRecognizers (PinchGestureRecognizer, PanGestureRecognizer)
- Mausrad-Support ist direkt in `OnPointerWheelChanged` implementiert
- Zoom-Faktor: 1.1x pro Mausrad-Schritt (10% Vergrößerung/Verkleinerung)

## 🔧 Programmatisch zoomen/pannen

### Zoom zurücksetzen

```csharp
ZoomPanCanvas.ResetZoomPan();
```

### Programmatisch zoomen

```csharp
// In ZoomPanCanvas.cs diese Methode hinzufügen:
public void SetZoom(double scale, double offsetX = 0, double offsetY = 0)
{
    _currentScale = Math.Max(MinScale, Math.Min(MaxScale, scale));
    _xOffset = offsetX;
    _yOffset = offsetY;
    _contentHost.Scale = _currentScale;
    _contentHost.TranslationX = _xOffset;
    _contentHost.TranslationY = _yOffset;
}

// Dann nutzen:
ZoomPanCanvas.SetZoom(1.5);  // 150% Zoom
ZoomPanCanvas.SetZoom(0.5, -100, -50);  // 50% Zoom, verschoben
```

## 🐛 Troubleshooting

### Problem: Elemente sind nicht sichtbar beim Rauszoomen

**Lösung:** Canvas-Größe ist zu klein

```csharp
// In ZoomPanCanvas.cs die Canvas-Größe erhöhen
_canvas.WidthRequest = 5000;   // Größer machen
_canvas.HeightRequest = 3000;  // Größer machen
```

### Problem: Zoom funktioniert nicht auf Android/iOS

**Lösung:** Handler nicht registriert

Überprüfen Sie `MauiProgram.cs` - die `.ConfigureMauiHandlers()` Zeilen müssen vorhanden sein!

### Problem: App laggt beim Zoomen

**Lösungen:**
1. **Weniger Elemente**: Optimal 10-50 Elemente, max 200
2. **Einfache Views**: Keine verschachtelten Layouts
3. **Release Build testen**: Debug-Builds sind 2-3x langsamer!

```bash
# Für Android Release Build:
dotnet publish -f net10.0-android -c Release
```

### Problem: Tap-Gesten reagieren nicht

**Lösung:** InputTransparent prüfen

```csharp
var element = new ContentView
{
    Content = myBorder,
    InputTransparent = false  // ← Wichtig!
};
```

## 💡 Best Practices

### Performance

**✅ Gut:**
```csharp
// Einfache View-Struktur
var border = new Border
{
    Content = new Label { Text = "Tisch 1" }
};
```

**❌ Schlecht:**
```csharp
// Verschachtelte Layouts vermeiden
var grid = new Grid
{
    Children = {
        new StackLayout {
            Children = { new Grid { /* ... */ } }
        }
    }
};
```

### Anzahl der Elemente

- ✅ **10-50 Elemente**: Butter-smooth
- ⚠️ **50-200 Elemente**: Noch flüssig
- ❌ **200+ Elemente**: Virtualisierung/Lazy Loading nutzen!

### Bilder optimieren

- Verwenden Sie komprimierte PNG/JPG
- Skalieren Sie auf tatsächlich benötigte Größe
- Vermeiden Sie transparente PNGs wenn möglich

## 📋 Vollständiges Beispiel

Siehe `src/TischplanApp/` für ein vollständiges, funktionierendes Beispiel mit:
- 6 Demo-Tischen
- Tap-Handlern
- Android, iOS und Windows Support
- Native Gesture Handlers

### Beispiel starten:

**Visual Studio 2026 Insiders:**
1. Öffne `TischplanApp.sln`
2. Wähle Platform (Windows/Android/iOS)
3. F5 drücken

**CLI:**
```bash
# Windows
dotnet build src/TischplanApp/TischplanApp.csproj -t:Run -f net10.0-windows10.0.19041.0

# Android
dotnet build src/TischplanApp/TischplanApp.csproj -t:Run -f net10.0-android

# iOS (nur macOS)
dotnet build src/TischplanApp/TischplanApp.csproj -t:Run -f net10.0-ios
```

## 📱 Fertige APK herunterladen

Sie können die vorgefertigte Android APK direkt aus diesem Repository herunterladen:

**[⬇️ TischplanApp.apk herunterladen](https://github.com/petarxxg/MauiScrollenZoomenView/raw/master/TischplanApp.apk)**

**APK Details:**
- **Framework**: .NET 10.0.100-rc.2
- **MAUI Version**: 10.0.0-preview
- **Größe**: 28 MB
- **Minimum Android**: API 21 (Android 5.0)
- **Features**:
  - ✅ Native Android Gesture Detectors
  - ✅ Buttery-smooth Pinch-to-Zoom
  - ✅ 6 Demo-Tische zum Testen
  - ✅ Tap-Handler mit Alerts

**Installation:**
1. APK auf Android-Gerät herunterladen
2. Installation aus unbekannten Quellen erlauben
3. APK installieren
4. App öffnen und Zoom/Pan testen!

## 🛠️ Technische Details

- **Framework**: .NET 10 MAUI
- **MAUI Version**: 10.0.0-preview
- **IDE**: Visual Studio 2026 Insiders (empfohlen)
- **Target Platforms**: Android, iOS, Windows
- **Minimum Android**: API 21 (Android 5.0)
- **Minimum iOS**: 14.2
- **Minimum Windows**: Windows 10 Build 19041

### Architektur

```
┌─────────────────────────────────────────┐
│         ZoomPanCanvas.cs                │  ← Shared Code (alle Plattformen)
│  - AbsoluteLayout (_canvas)             │
│  - ContentView (_contentHost)           │
│  - Scale + TranslationX/Y Management    │
└─────────────────────────────────────────┘
                   │
      ┌────────────┼────────────┐
      │            │            │
      ▼            ▼            ▼
┌──────────┐ ┌──────────┐ ┌──────────┐
│ Android  │ │   iOS    │ │ Windows  │
│  Handler │ │  Handler │ │  MAUI    │
├──────────┤ ├──────────┤ │ Gestures │
│ Scale    │ │ UIPinch  │ │  + Mouse │
│ Gesture  │ │ Gesture  │ │  Wheel   │
│ Detector │ │ Recogniz.│ │          │
│          │ │          │ │          │
│ Gesture  │ │ UIPan    │ │          │
│ Detector │ │ Gesture  │ │          │
└──────────┘ └──────────┘ └──────────┘
     │            │            │
     └────────────┴────────────┘
              │
              ▼
    Proportionale Skalierung:
    translation *= (newScale / oldScale)
```

## 📝 Changelog

### Version 2.1 - Android Emulator Mausrad-Support (27. Oktober 2025)
- ✅ **Neue Features**
  - Android Handler: Mausrad-Zoom im Emulator funktioniert jetzt! 🖱️
  - GenericMotionEvent Handler für Axis.Vscroll hinzugefügt
  - Zoom-Faktor: 1.1x pro Mausrad-Schritt (wie Windows)
  - Perfekt für Desktop-Testing im Android Emulator

- ✅ **Zwei Zoom-Methoden im Emulator**
  - **Methode 1**: Mausrad nach oben/unten (neu, direkt, einfachste Methode)
  - **Methode 2**: Strg + Linke Maustaste + Ziehen (Standard Android Emulator Pinch-Simulation)
  - Beide Methoden funktionieren parallel!

- ✅ **Verbesserte Bedienung**
  - Kein "nach oben/unten wischen" mehr beim Mausrad-Scrollen
  - Pan: Linke Maustaste (ohne Strg) + Ziehen
  - README erweitert mit detaillierter Emulator-Bedienungsanleitung

### Version 2.0 - .NET 10 Migration (27. Oktober 2025)
- ✅ **Migration auf .NET 10**
  - TargetFrameworks: net9.0 → net10.0 (Android, iOS, Windows)
  - MAUI Packages: 9.0.10 → 10.0.0-preview
  - SDK: .NET 10.0.100-rc.2.25502.107

- ✅ **Breaking Changes behoben**
  - App.xaml.cs: `MainPage` → `CreateWindow()` überschrieben
  - ZoomPanCanvas.cs: `Application.MainPage` → `Windows[0].Page`
  - ZoomPanCanvas.cs: `DisplayAlert` → `DisplayAlertAsync`
  - iOS Handler: ContentView Namenskonflikt mit alias behoben

- ✅ **Build-Status**
  - 0 Warnungen, 0 Fehler
  - Vollständig kompatibel mit Visual Studio 2026 Insiders
  - APK neu gebaut (28 MB)

### Version 1.0 - Initial Release
- ✅ Native Android/iOS Gesture Handlers
- ✅ Proportionale Zoom-Skalierung
- ✅ Windows Mouse Wheel Support
- ✅ Buttery-smooth Performance
- ✅ Production-ready Code

## 📄 Lizenz

Dieses Projekt ist Open Source und für kommerzielle und private Projekte frei nutzbar.

## 🤝 Beiträge

Verbesserungen und Bug-Fixes sind willkommen! Erstellen Sie einfach einen Pull Request.

---

**Viel Erfolg bei der Integration in Ihre App!** 🎉

Bei Fragen: Issue erstellen oder die Demo-App als Referenz nutzen.
