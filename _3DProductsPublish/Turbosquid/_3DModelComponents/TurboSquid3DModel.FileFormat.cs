using Tomlyn.Syntax;
using static Tomlyn.Helpers.TomlNamingHelper;

namespace _3DProductsPublish.Turbosquid._3DModelComponents;

enum FileFormat
{
    alias_wire,
    autocad_drawing,
    blender,
    bryce,
    cinema_4d,
    collada,
    cryengine,
    directx,
    dxf,
    electric_image,
    fbx,
    formz,
    gltf,
    gmax,
    iges,
    inventor_assembly,
    inventor_drawing,
    inventor_part,
    jsr184,
    lightwave,
    marvelous_designer,
    maya,
    microsoft_directdraw_surface,
    microstation_drawing_file,
    modo,
    mudbox,
    obj,
    openflight,
    other,
    poser,
    pulse,
    quake_iii,
    quickdraw_3d,
    renderman,
    revit_design,
    revit_family,
    rhino,
    shockwave_3d,
    sketchup,
    softimage,
    solidworks_assembly,
    solidworks_part,
    stl,
    strata_3d,
    terragen,
    truespace,
    truespace_object,
    truespace_scene,
    unity,
    unreal,
    usd,
    usdz,
    vrml,
    vue,
    zbrush,
    zw3d,
    _3ds_max,
    _3d_studio,
    _3d_studio_project
}

static class FileFormatExtensions
{
    internal static bool IsNative(this FileFormat fileFormat)
        => fileFormat is
        FileFormat._3ds_max or
        FileFormat.blender or
        FileFormat.cinema_4d or
        FileFormat.lightwave or
        FileFormat.maya or
        FileFormat.softimage or
        FileFormat.usd;

    internal static string ToString_(this FileFormat fileFormat)
        => fileFormat.ToString().TrimStart('_');
}

abstract class NativeFileFormatMetadata
{
    public FileFormat FileFormat { get; }
    public double FormatVersion { get; }
    public string Renderer { get; }
    public double? RendererVersion { get; }

    protected NativeFileFormatMetadata(FileFormat fileFormat, double formatVersion = 1.0, string? renderer = null, double? rendererVersion = null)
    {
        FileFormat = fileFormat;
        FormatVersion = formatVersion;
        Renderer = renderer ?? "other";
        RendererVersion = rendererVersion;
    }
}
abstract class NativeFileFormatMetadata<TRenderer> : NativeFileFormatMetadata where TRenderer : struct, Enum
{
    protected NativeFileFormatMetadata(FileFormat fileFormat, double formatVersion = 1.0, TRenderer? renderer = null, double? rendererVersion = null)
        : base(fileFormat, formatVersion, renderer?.ToString(), rendererVersion) { }
}
static class NativeFileFormatMetadataExtensions
{
    internal static void Add(this SyntaxList<KeyValueSyntax> tableItems, NativeFileFormatMetadata nativeFileFormatMetadata)
    {
        tableItems.Add(PascalToSnakeCase(nameof(TurboSquid3DModelMetadata.FormatVersion)), nativeFileFormatMetadata.FormatVersion);
        tableItems.Add(PascalToSnakeCase(nameof(TurboSquid3DModelMetadata.Renderer)), nativeFileFormatMetadata.Renderer);
        if (nativeFileFormatMetadata.RendererVersion is double rendererVersion)
            tableItems.Add(PascalToSnakeCase(nameof(TurboSquid3DModelMetadata.RendererVersion)), rendererVersion);
    }
}

class _3ds_max : NativeFileFormatMetadata<_3ds_max.Renderer_>
{
    public _3ds_max(double formatVersion = 1.0, Renderer_? renderer = null, double? rendererVersion = null)
        : base(FileFormat._3ds_max, formatVersion, renderer, rendererVersion)
    {
    }

    internal enum Renderer_
    {
        arion,
        arnold,
        brazil,
        corona,
        defaul_scanline,
        finalrender,
        indigo,
        iray,
        krakatoa,
        maxwell,
        mental_ray,
        quicksilver,
        redshift,
        vray
    }
}

class blender : NativeFileFormatMetadata<blender.Renderer_>
{
    public blender(double formatVersion = 1.0, Renderer_? renderer = null, double? rendererVersion = null)
        : base(FileFormat.blender, formatVersion, renderer, rendererVersion)
    {
    }

    internal enum Renderer_
    {
        blender_render,
        cycles_render,
        eevee_renderer
    }
}

class cinema_4d : NativeFileFormatMetadata<cinema_4d.Renderer_>
{
    public cinema_4d(double formatVersion = 1.0, Renderer_? renderer = null, double? rendererVersion = null)
    : base(FileFormat.cinema_4d, formatVersion, renderer, rendererVersion)
    {
    }

    internal enum Renderer_
    {
        advanced_render,
        arion,
        arnold,
        corona,
        default_scanline,
        finalrender,
        indigo,
        maxwell,
        mental_ray,
        octane,
        physical,
        redshift,
        vray,
        vrayforc4d
    }
}

class lightwave : NativeFileFormatMetadata<lightwave.Renderer_>
{
    public lightwave(double formatVersion = 1.0, Renderer_? renderer = null, double? rendererVersion = null)
        : base(FileFormat.lightwave, formatVersion, renderer, rendererVersion)
    {
    }

    internal enum Renderer_
    {
        arion,
        default_scanline,
        fprime,
        maxwell
    }
}

class maya : NativeFileFormatMetadata<maya.Renderer_>
{
    public maya(double formatVersion = 1.0, Renderer_? renderer = null, double? rendererVersion = null)
    : base(FileFormat.maya, formatVersion, renderer, rendererVersion)
    {
    }

    internal enum Renderer_
    {
        _3delight,
        arion,
        arnold,
        default_scanline,
        finalrender,
        indigo,
        maxwell,
        maya_hardware,
        maya_software,
        mental_ray,
        redshift,
        renderman,
        vray
    }
}

class softimage : NativeFileFormatMetadata<softimage.Renderer_>
{
    public softimage(double formatVersion = 1.0, Renderer_? renderer = null, double? rendererVersion = null)
        : base(FileFormat.softimage, formatVersion, renderer, rendererVersion)
    {
    }

    internal enum Renderer_
    {
        _3delight,
        arion,
        arnold,
        default_scanline,
        maxwell,
        mental_ray,
        softimage_hardware,
        vray
    }
}

class usd : NativeFileFormatMetadata<usd.Renderer_>
{
    public const string Name = "usd";

    public usd(double formatVersion = 1.0, Renderer_? renderer = null, double? rendererVersion = null)
        : base(FileFormat.usd, formatVersion, renderer, rendererVersion)
    {
    }

    internal enum Renderer_
    {
        omniverse
    }
}
