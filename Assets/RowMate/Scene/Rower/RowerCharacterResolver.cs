using System;
using System.Collections.Generic;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Stroke;
using RowMate.Scene.Slots;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RowMate.Scene
{
    // Qué remero se arma (SceneHostOptions.Character y SceneHost.ForceCharacterForTest): Auto = el binding de la app (Resources) o el
    // maniquí; Mannequin = siempre el maniquí (las pruebas y el arnés, iguales en todas las máquinas); Slot = el binding (o el que se pasa);
    // InvalidForTest = un GameObject sin avatar (cae al maniquí con un aviso).
    public enum RowerCharacterKind
    {
        Auto,
        Mannequin,
        Slot,
        InvalidForTest,
    }

    // El remero armado en el bote: la raíz (girada 180°: mira a la cámara), sus huesos, el driver, sus largos y de dónde salió.
    public sealed class RowerCharacter : IDisposable
    {
        private readonly List<Object> _owned = new List<Object>();
        private SkinnedMeshRenderer[]? _renderers;

        internal RowerCharacter(GameObject root, RowerBones bones, RowerRigDriver driver, in RowerSpec spec, bool fromSlot, string source)
        {
            Root = root;
            Bones = bones;
            Driver = driver;
            Spec = spec;
            FromSlot = fromSlot;
            Source = source;
        }

        public GameObject Root { get; }

        public RowerBones Bones { get; }

        public RowerRigDriver Driver { get; }

        public RowerSpec Spec { get; }

        // true = el personaje del slot (Y Bot o el realista); false = el maniquí.
        public bool FromSlot { get; }

        // "maniquí" o la ruta del proyecto del FBX.
        public string Source { get; }

        internal List<Object> Owned => _owned;

        public void CastShadows(bool on)
        {
            var mode = on ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderers ??= Root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var renderer in _renderers)
            {
                renderer.shadowCastingMode = mode;
            }
        }

        // Los triángulos que dibuja (el presupuesto).
        public long Triangles
        {
            get
            {
                var total = 0L;
                _renderers ??= Root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var renderer in _renderers)
                {
                    var mesh = renderer.sharedMesh;
                    if (mesh == null)
                    {
                        continue;
                    }

                    for (var i = 0; i < mesh.subMeshCount; i++)
                    {
                        total += (long)mesh.GetIndexCount(i) / 3;
                    }
                }

                return total;
            }
        }

        public void Dispose()
        {
            MeshUpload.Destroy(Root);
            foreach (var obj in _owned)
            {
                MeshUpload.Destroy(obj);
            }

            _owned.Clear();
        }
    }

    // La resolución del remero (07-remero-palada/02-tech-design §3.2; DT7-13; AC-SCN-08-08): sin binding (el CI, un clon sin Mixamo) el
    // maniquí sin aviso; con binding, su modelo si valida (RowerCharacterValidation) y tiene los huesos del cuerpo; si el binding quedó sin
    // modelo (el FBX se borró) o no valida, el maniquí y UN aviso "[SLOT] rower.character: <motivo>" (SlotLog). Nunca un error. El
    // personaje del slot: instanciado bajo el marco del bote, girado 180°, el Animator apagado, los dedos en el agarre y la ropa horneada.
    public static class RowerCharacterResolver
    {
        // loadFromResources = false: sin binding dado, el maniquí (las pruebas de "sin binding" en una máquina que tiene Y Bot).
        public static RowerCharacter Resolve(RowerCharacterKind kind, RowerCharacterBinding? binding, Transform boatFrame, int layer, Material material,
                                             SlotLog log, bool loadFromResources = true)
        {
            if (kind == RowerCharacterKind.Mannequin)
            {
                return Mannequin(boatFrame, layer, material);
            }

            GameObject? model;
            string source;
            GameObject? scratch = null;
            if (kind == RowerCharacterKind.InvalidForTest)
            {
                scratch = new GameObject("rower-invalid-for-test") { hideFlags = HideFlags.HideAndDontSave };
                model = scratch;
                source = "InvalidForTest";
            }
            else
            {
                if (binding == null && loadFromResources)
                {
                    binding = RowerCharacterBinding.Load();
                }
                if (binding == null)
                {
                    return Mannequin(boatFrame, layer, material);
                }

                model = binding.Model;
                source = binding.SourcePath;
                if (model == null)
                {
                    log.Warn(SlotIds.RowerCharacter, "el binding apunta a " + source + " y el FBX no está: se usa el maniquí");
                    return Mannequin(boatFrame, layer, material);
                }
            }

            try
            {
                if (!RowerCharacterValidation.Validate(model, out var reason, out _))
                {
                    log.Warn(SlotIds.RowerCharacter, reason + " (" + source + "): se usa el maniquí");
                    return Mannequin(boatFrame, layer, material);
                }

                var instance = Object.Instantiate(model, boatFrame, false);
                instance.name = "rower-slot";
                instance.hideFlags = HideFlags.DontSave;
                SetLayer(instance.transform, layer);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.Euler(0, 180, 0);
                foreach (var animator in instance.GetComponentsInChildren<Animator>(true))
                {
                    animator.enabled = false;
                }

                var bones = RowerBones.Find(instance.transform);
                var missing = bones.Missing();
                if (missing.Length > 0)
                {
                    MeshUpload.Destroy(instance);
                    log.Warn(SlotIds.RowerCharacter, "faltan huesos del cuerpo (" + missing + ") en " + source + ": se usa el maniquí");
                    return Mannequin(boatFrame, layer, material);
                }

                var spec = RowerRigDriver.MeasureSpec(bones);
                var driver = new RowerRigDriver(bones, boatFrame);
                driver.Initialize();
                driver.ApplyGrip();
                var character = new RowerCharacter(instance, bones, driver, spec, true, source);
                foreach (var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    ProceduralRower.Configure(renderer, spec.Height);
                    renderer.rootBone = bones[RowerJoint.Hips];
                    renderer.enabled = false;
                }

                return character;
            }
            finally
            {
                MeshUpload.Destroy(scratch);
            }
        }

        // La ropa del personaje del slot (su propia unidad del armado: copia las mallas). Hasta acá sus renderers quedan apagados.
        public static void Dress(RowerCharacter character, Material material)
        {
            if (!character.FromSlot)
            {
                return;
            }

            RowerOutfitBaker.Bake(character.Root, material, character.Owned);
            foreach (var renderer in character.Root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                renderer.enabled = true;
            }
        }

        public static RowerCharacter Mannequin(Transform boatFrame, int layer, Material material)
        {
            var spec = RowerSpec.Mannequin;
            var root = ProceduralRower.Create(boatFrame, layer, spec, material, out var mesh);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.Euler(0, 180, 0);
            var bones = RowerBones.Find(root.transform);
            var driver = new RowerRigDriver(bones, boatFrame);
            driver.Initialize();
            var character = new RowerCharacter(root, bones, driver, spec, false, "maniquí");
            character.Owned.Add(mesh);
            return character;
        }

        private static void SetLayer(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            for (var i = 0; i < t.childCount; i++)
            {
                SetLayer(t.GetChild(i), layer);
            }
        }
    }
}
