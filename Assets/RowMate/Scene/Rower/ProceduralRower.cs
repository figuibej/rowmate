using RowMate.Core.Scene.Geometry;
using RowMate.Core.Scene.Stroke;
using UnityEngine;
using UnityEngine.Rendering;

namespace RowMate.Scene
{
    // El maniquí procedural (07-remero-palada/02-tech-design §1.2, §2.13, §3.2; DA7-03; DT7-09): el esqueleto mixamorig:* del cuerpo de
    // MannequinSkeleton (T-pose, mira a +z) como Transform y la malla de MannequinMesh con skinning (2 huesos por vértice, la región en el
    // rojo del color de vértice) en un SkinnedMeshRenderer con el material Rower. Sin dedos (el puño es rígido). Es el remero por defecto:
    // la app entera funciona sin ningún asset de terceros.
    public static class ProceduralRower
    {
        public const string RootName = "rower-mannequin";

        // Arma el maniquí bajo `parent` (sin girarlo: lo gira quien lo pone en el bote). La malla queda en `mesh` (la destruye el mundo).
        public static GameObject Create(Transform parent, int layer, in RowerSpec spec, Material material, out Mesh mesh)
        {
            var root = new GameObject(RootName) { hideFlags = HideFlags.DontSave, layer = layer };
            root.transform.SetParent(parent, false);
            var skeleton = MannequinSkeleton.Build(spec);
            var transforms = new Transform[skeleton.Length];
            for (var i = 0; i < skeleton.Length; i++)
            {
                var bone = skeleton[i];
                var go = new GameObject(bone.Name) { hideFlags = HideFlags.DontSave, layer = layer };
                var parentTransform = bone.Parent >= 0 ? transforms[bone.Parent] : root.transform;
                go.transform.SetParent(parentTransform, false);
                var parentPosition = bone.Parent >= 0 ? skeleton[bone.Parent].Position : SceneVec3.Zero;
                go.transform.localPosition = RowerRigDriver.ToVector(bone.Position - parentPosition);
                transforms[i] = go.transform;
            }

            var data = new SkinnedMeshData(MannequinMesh.VertexCapacity, MannequinMesh.IndexCapacity);
            MannequinMesh.Build(spec, data);
            mesh = MeshUpload.NewMesh("rower-mannequin");
            using (var upload = new MeshUpload())
            {
                upload.Upload(data.Mesh, mesh, false, null, true);
            }

            var weights = new BoneWeight[data.VertexCount];
            for (var v = 0; v < weights.Length; v++)
            {
                weights[v] = new BoneWeight
                {
                    boneIndex0 = data.Bone0[v],
                    boneIndex1 = data.Bone1[v],
                    weight0 = data.Weight0[v],
                    weight1 = data.Weight1[v],
                };
            }

            mesh.boneWeights = weights;
            var bindposes = new Matrix4x4[transforms.Length];
            var rootToWorld = root.transform.localToWorldMatrix;
            for (var i = 0; i < transforms.Length; i++)
            {
                bindposes[i] = transforms[i].worldToLocalMatrix * rootToWorld;
            }

            mesh.bindposes = bindposes;
            mesh.bounds = new Bounds(new Vector3(0, (float)(spec.Height / 2), 0), new Vector3(2.2f, (float)spec.Height + 0.4f, 2.2f));

            var meshObject = new GameObject("rower-mannequin-mesh") { hideFlags = HideFlags.DontSave, layer = layer };
            meshObject.transform.SetParent(root.transform, false);
            var renderer = meshObject.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = mesh;
            renderer.bones = transforms;
            renderer.rootBone = transforms[MannequinSkeleton.Hips];
            renderer.sharedMaterial = material;
            Configure(renderer, spec.Height);
            renderer.quality = SkinQuality.Bone2;
            return root;
        }

        // Lo común a los SkinnedMeshRenderer del remero (el maniquí y el personaje del slot): sombras, sin sondas, bounds fijos alrededor
        // de la cadera (sin recalcularlos en cada cuadro) y siempre al día.
        public static void Configure(SkinnedMeshRenderer renderer, double height)
        {
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            renderer.allowOcclusionWhenDynamic = false;
            renderer.skinnedMotionVectors = false;
            renderer.updateWhenOffscreen = false;
            renderer.forceMatrixRecalculationPerRender = false;
            var size = (float)height + 0.6f;
            renderer.localBounds = new Bounds(Vector3.zero, new Vector3(size, size, size));
            renderer.quality = SkinQuality.Auto;
        }
    }
}
