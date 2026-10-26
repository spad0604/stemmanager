# Third-party notices

The release bundles third-party runtime libraries and unmodified pre-trained models. No model was trained by this project. See `models-manifest.json` for SHA-256 and origins. YuNet: copyright (c) 2020 Shiqi Yu; full MIT license in `YuNet-MIT.txt`. SFace from OpenCV Zoo: full Apache 2.0 terms in `SFace-APACHE-2.0.txt`.

| Dependency | License / source |
|---|---|
| .NET runtime, Microsoft extensions, EF Core | MIT, https://github.com/dotnet/runtime and https://github.com/dotnet/efcore |
| Avalonia | MIT, https://github.com/AvaloniaUI/Avalonia |
| CommunityToolkit.Mvvm | MIT, https://github.com/CommunityToolkit/dotnet |
| SQLite | Public domain, https://sqlite.org/copyright.html |
| SQLitePCLRaw | Apache-2.0, https://github.com/ericsink/SQLitePCL.raw |
| ClosedXML | MIT, https://github.com/ClosedXML/ClosedXML |
| Open XML SDK | MIT, https://github.com/dotnet/Open-XML-SDK |
| QRCoder | MIT, https://github.com/codebude/QRCoder |
| SkiaSharp | MIT, https://github.com/mono/SkiaSharp; underlying Skia BSD-3-Clause |
| ONNX Runtime | MIT, https://github.com/microsoft/onnxruntime |
| OpenCvSharp | Apache-2.0, https://github.com/shimat/opencvsharp |
| OpenCV | Apache-2.0, https://github.com/opencv/opencv |
| Inter | SIL Open Font License 1.1, https://github.com/rsms/inter |

NuGet package inventories can be generated with `dotnet list STEMManager.sln package --include-transitive --format json`. The installer itself is compiled by Inno Setup and must retain its required notices. Test tools and EF design tooling are development dependencies and are not application features. Verify upstream redistribution notices and native payloads when preparing a signed public release.
