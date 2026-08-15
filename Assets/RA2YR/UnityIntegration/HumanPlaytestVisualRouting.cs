using System;
using System.Collections.Generic;
using System.Linq;

namespace RA2YR.UnityIntegration
{
    public enum HumanPlaytestVisualRole
    {
        HumanBasicUnit,
        HumanHarvester,
        HumanBase,
        HumanRefinery,
        HumanFactory,
        HumanPower,
        EnemyBasicUnit,
        EnemyBase,
        EnemyFactory
    }

    public enum HumanPlaytestRulesRegistry
    {
        InfantryTypes,
        VehicleTypes,
        BuildingTypes,
        AircraftTypes
    }

    public enum HumanPlaytestVisualFormat
    {
        Shp,
        VxlStatic,
        VxlHva
    }

    public enum HumanPlaytestRemapProfile
    {
        SourcePaletteOnly,
        ImplementationSpecificConfigured
    }

    public sealed class HumanPlaytestVisualRoleBinding
    {
        public HumanPlaytestVisualRoleBinding(
            HumanPlaytestVisualRole role,
            HumanPlaytestRulesRegistry registry,
            string typeId)
        {
            if (!Enum.IsDefined(typeof(HumanPlaytestVisualRole), role) ||
                !Enum.IsDefined(typeof(HumanPlaytestRulesRegistry), registry))
            {
                throw new ArgumentOutOfRangeException(nameof(role));
            }

            if (string.IsNullOrWhiteSpace(typeId))
            {
                throw new ArgumentException("A logical Rules type id is required.", nameof(typeId));
            }

            Role = role;
            Registry = registry;
            TypeId = typeId.Trim();
        }

        public HumanPlaytestVisualRole Role { get; }
        public HumanPlaytestRulesRegistry Registry { get; }
        public string TypeId { get; }
    }

    public sealed class HumanPlaytestVisualRoleProfile
    {
        private readonly IReadOnlyList<HumanPlaytestVisualRoleBinding> bindings;

        public HumanPlaytestVisualRoleProfile(
            IEnumerable<HumanPlaytestVisualRoleBinding> bindings,
            string defaultPaletteLogicalName = "unittem.pal",
            HumanPlaytestRemapProfile remapProfile = HumanPlaytestRemapProfile.SourcePaletteOnly)
        {
            HumanPlaytestVisualRoleBinding[] values =
                (bindings ?? throw new ArgumentNullException(nameof(bindings))).ToArray();
            if (values.Length == 0 || values.Any(value => value == null) ||
                values.Select(value => value.Role).Distinct().Count() != values.Length)
            {
                throw new ArgumentException("Visual role bindings must be unique and non-empty.", nameof(bindings));
            }

            if (string.IsNullOrWhiteSpace(defaultPaletteLogicalName) ||
                !Enum.IsDefined(typeof(HumanPlaytestRemapProfile), remapProfile))
            {
                throw new ArgumentException("A valid palette and remap profile are required.");
            }

            this.bindings = Array.AsReadOnly(values);
            DefaultPaletteLogicalName = defaultPaletteLogicalName.Trim();
            RemapProfile = remapProfile;
        }

        public IReadOnlyList<HumanPlaytestVisualRoleBinding> Bindings => bindings;
        public string DefaultPaletteLogicalName { get; }
        public HumanPlaytestRemapProfile RemapProfile { get; }

        public HumanPlaytestVisualRoleBinding Find(HumanPlaytestVisualRole role)
        {
            return bindings.FirstOrDefault(value => value.Role == role);
        }

        public static HumanPlaytestVisualRoleProfile CreateDefault()
        {
            return new HumanPlaytestVisualRoleProfile(new[]
            {
                new HumanPlaytestVisualRoleBinding(HumanPlaytestVisualRole.HumanBasicUnit, HumanPlaytestRulesRegistry.InfantryTypes, "E1"),
                new HumanPlaytestVisualRoleBinding(HumanPlaytestVisualRole.HumanHarvester, HumanPlaytestRulesRegistry.VehicleTypes, "HARV"),
                new HumanPlaytestVisualRoleBinding(HumanPlaytestVisualRole.HumanBase, HumanPlaytestRulesRegistry.BuildingTypes, "GACNST"),
                new HumanPlaytestVisualRoleBinding(HumanPlaytestVisualRole.HumanRefinery, HumanPlaytestRulesRegistry.BuildingTypes, "PROC"),
                new HumanPlaytestVisualRoleBinding(HumanPlaytestVisualRole.HumanFactory, HumanPlaytestRulesRegistry.BuildingTypes, "WEAP"),
                new HumanPlaytestVisualRoleBinding(HumanPlaytestVisualRole.HumanPower, HumanPlaytestRulesRegistry.BuildingTypes, "GAPOWR"),
                new HumanPlaytestVisualRoleBinding(HumanPlaytestVisualRole.EnemyBasicUnit, HumanPlaytestRulesRegistry.InfantryTypes, "E1"),
                new HumanPlaytestVisualRoleBinding(HumanPlaytestVisualRole.EnemyBase, HumanPlaytestRulesRegistry.BuildingTypes, "NACNST"),
                new HumanPlaytestVisualRoleBinding(HumanPlaytestVisualRole.EnemyFactory, HumanPlaytestRulesRegistry.BuildingTypes, "NAWEAP")
            });
        }
    }

    public sealed class HumanPlaytestRoleDescriptor
    {
        public HumanPlaytestRoleDescriptor(
            HumanPlaytestVisualRole role,
            string typeId,
            string imageLogicalName,
            bool voxel,
            string paletteLogicalName)
        {
            Role = role;
            TypeId = string.IsNullOrWhiteSpace(typeId) ? throw new ArgumentException(nameof(typeId)) : typeId;
            ImageLogicalName = string.IsNullOrWhiteSpace(imageLogicalName) ? throw new ArgumentException(nameof(imageLogicalName)) : imageLogicalName;
            Voxel = voxel;
            PaletteLogicalName = string.IsNullOrWhiteSpace(paletteLogicalName) ? null : paletteLogicalName;
        }

        public HumanPlaytestVisualRole Role { get; }
        public string TypeId { get; }
        public string ImageLogicalName { get; }
        public bool Voxel { get; }
        public string PaletteLogicalName { get; }
    }

    public sealed class HumanPlaytestAssetAvailability
    {
        public HumanPlaytestAssetAvailability(
            string logicalName,
            bool hasShp,
            bool hasVxl,
            bool hasHva,
            bool hasPalette)
        {
            LogicalName = string.IsNullOrWhiteSpace(logicalName) ? throw new ArgumentException(nameof(logicalName)) : logicalName;
            HasShp = hasShp;
            HasVxl = hasVxl;
            HasHva = hasHva;
            HasPalette = hasPalette;
        }

        public string LogicalName { get; }
        public bool HasShp { get; }
        public bool HasVxl { get; }
        public bool HasHva { get; }
        public bool HasPalette { get; }
    }

    public enum HumanPlaytestRoleDiagnosticCode
    {
        MissingRulesType,
        MissingArtDescriptor,
        MissingVisualAsset,
        MissingPalette,
        HvaBindingMissing
    }

    public sealed class HumanPlaytestRoleDiagnostic
    {
        internal HumanPlaytestRoleDiagnostic(
            HumanPlaytestVisualRole role,
            HumanPlaytestRoleDiagnosticCode code)
        {
            Role = role;
            Code = code;
        }

        public HumanPlaytestVisualRole Role { get; }
        public HumanPlaytestRoleDiagnosticCode Code { get; }
    }

    public sealed class ResolvedLegacyVisual
    {
        internal ResolvedLegacyVisual(
            HumanPlaytestVisualRole role,
            HumanPlaytestVisualFormat format,
            string visualAssetId,
            string imageLogicalName,
            string paletteLogicalName,
            bool hvaBound)
        {
            Role = role;
            Format = format;
            VisualAssetId = visualAssetId;
            ImageLogicalName = imageLogicalName;
            PaletteLogicalName = paletteLogicalName;
            HvaBound = hvaBound;
        }

        public HumanPlaytestVisualRole Role { get; }
        public HumanPlaytestVisualFormat Format { get; }
        public string VisualAssetId { get; }
        public string ImageLogicalName { get; }
        public string PaletteLogicalName { get; }
        public bool HvaBound { get; }
    }

    public sealed class HumanPlaytestRoleResolutionResult
    {
        private readonly IReadOnlyList<ResolvedLegacyVisual> resolved;
        private readonly IReadOnlyList<HumanPlaytestVisualRole> unresolved;
        private readonly IReadOnlyList<HumanPlaytestRoleDiagnostic> diagnostics;

        internal HumanPlaytestRoleResolutionResult(
            IEnumerable<ResolvedLegacyVisual> resolved,
            IEnumerable<HumanPlaytestVisualRole> unresolved,
            IEnumerable<HumanPlaytestRoleDiagnostic> diagnostics)
        {
            this.resolved = Array.AsReadOnly((resolved ?? Enumerable.Empty<ResolvedLegacyVisual>()).ToArray());
            this.unresolved = Array.AsReadOnly((unresolved ?? Enumerable.Empty<HumanPlaytestVisualRole>()).ToArray());
            this.diagnostics = Array.AsReadOnly((diagnostics ?? Enumerable.Empty<HumanPlaytestRoleDiagnostic>()).ToArray());
        }

        public IReadOnlyList<ResolvedLegacyVisual> Resolved => resolved;
        public IReadOnlyList<HumanPlaytestVisualRole> Unresolved => unresolved;
        public IReadOnlyList<HumanPlaytestRoleDiagnostic> Diagnostics => diagnostics;

        public ResolvedLegacyVisual Find(HumanPlaytestVisualRole role)
        {
            return resolved.FirstOrDefault(value => value.Role == role);
        }
    }

    public static class HumanPlaytestVisualRoleResolver
    {
        public static HumanPlaytestRoleResolutionResult Resolve(
            HumanPlaytestVisualRoleProfile profile,
            IEnumerable<HumanPlaytestRoleDescriptor> descriptors,
            IEnumerable<HumanPlaytestAssetAvailability> assets)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            HumanPlaytestRoleDescriptor[] descriptorArray = (descriptors ?? throw new ArgumentNullException(nameof(descriptors))).ToArray();
            HumanPlaytestAssetAvailability[] assetArray = (assets ?? throw new ArgumentNullException(nameof(assets))).ToArray();
            var byName = assetArray.GroupBy(value => value.LogicalName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(value => value.Key, value => value.First(), StringComparer.OrdinalIgnoreCase);
            var resolved = new List<ResolvedLegacyVisual>();
            var unresolved = new List<HumanPlaytestVisualRole>();
            var diagnostics = new List<HumanPlaytestRoleDiagnostic>();

            foreach (HumanPlaytestVisualRoleBinding binding in profile.Bindings.OrderBy(value => value.Role))
            {
                HumanPlaytestRoleDescriptor descriptor = descriptorArray.FirstOrDefault(value =>
                    value.Role == binding.Role && string.Equals(value.TypeId, binding.TypeId, StringComparison.OrdinalIgnoreCase));
                if (descriptor == null)
                {
                    unresolved.Add(binding.Role);
                    diagnostics.Add(new HumanPlaytestRoleDiagnostic(binding.Role, HumanPlaytestRoleDiagnosticCode.MissingArtDescriptor));
                    continue;
                }

                string extension = descriptor.Voxel ? ".vxl" : ".shp";
                string imageName = EnsureExtension(descriptor.ImageLogicalName, extension);
                HumanPlaytestAssetAvailability image = FindAsset(byName, imageName);
                string paletteName = descriptor.PaletteLogicalName ?? profile.DefaultPaletteLogicalName;
                HumanPlaytestAssetAvailability palette = FindAsset(byName, paletteName);
                if (palette == null || !palette.HasPalette)
                {
                    unresolved.Add(binding.Role);
                    diagnostics.Add(new HumanPlaytestRoleDiagnostic(binding.Role, HumanPlaytestRoleDiagnosticCode.MissingPalette));
                    continue;
                }

                if (descriptor.Voxel)
                {
                    if (image == null || !image.HasVxl)
                    {
                        unresolved.Add(binding.Role);
                        diagnostics.Add(new HumanPlaytestRoleDiagnostic(binding.Role, HumanPlaytestRoleDiagnosticCode.MissingVisualAsset));
                        continue;
                    }

                    bool hvaBound = image.HasHva;
                    if (!hvaBound)
                    {
                        diagnostics.Add(new HumanPlaytestRoleDiagnostic(binding.Role, HumanPlaytestRoleDiagnosticCode.HvaBindingMissing));
                    }

                    resolved.Add(new ResolvedLegacyVisual(
                        binding.Role,
                        hvaBound ? HumanPlaytestVisualFormat.VxlHva : HumanPlaytestVisualFormat.VxlStatic,
                        "external-legacy/playtest/" + binding.Role,
                        imageName,
                        paletteName,
                        hvaBound));
                    continue;
                }

                if (image == null || !image.HasShp)
                {
                    unresolved.Add(binding.Role);
                    diagnostics.Add(new HumanPlaytestRoleDiagnostic(binding.Role, HumanPlaytestRoleDiagnosticCode.MissingVisualAsset));
                    continue;
                }

                resolved.Add(new ResolvedLegacyVisual(
                    binding.Role,
                    HumanPlaytestVisualFormat.Shp,
                    "external-legacy/playtest/" + binding.Role,
                    imageName,
                    paletteName,
                    false));
            }

            return new HumanPlaytestRoleResolutionResult(resolved, unresolved, diagnostics);
        }

        private static HumanPlaytestAssetAvailability FindAsset(
            IReadOnlyDictionary<string, HumanPlaytestAssetAvailability> assets,
            string logicalName)
        {
            HumanPlaytestAssetAvailability value;
            return assets.TryGetValue(logicalName, out value) ? value : null;
        }

        private static string EnsureExtension(string name, string extension)
        {
            return name.EndsWith(extension, StringComparison.OrdinalIgnoreCase)
                ? name
                : name + extension;
        }
    }
}
