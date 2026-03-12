using Silk.NET.Assimp;
using Silk.NET.OpenGL;
using Phoenix.Rendering.Geometry;
using System.Numerics;
namespace Phoenix.Rendering.Animation
{
    public class AnimatedModel : IDisposable
    {
        public List<ModelPart> Parts { get; private set; }
        public AnimatorNode[] AnimatorNodes;

        private readonly GL GL;
        private Model _model;
        private Assimp _assimp;

        public Animator Animator { get; private set; }
        private bool _modelHierarchySet = false;
        private List<AnimatorNode> _animatorNodes = new List<AnimatorNode>();
        public AnimatedModel(GL gl, string modelPath, string animationsPath, string[] animationNames, bool extractTextures = false)
        {
            if (animationNames.Count() == 0)
                throw new Exception("At least one animation is required");

            GL = gl;
            _assimp = Assimp.GetApi();
            _model = new Model(gl: GL, path: modelPath,
                meshAttributes:
                    MeshAttributes.Position3D
                    | MeshAttributes.TexCoord
                    | MeshAttributes.Normals
                    | MeshAttributes.boneIds
                    | MeshAttributes.boneWeights,
                extractTextures: extractTextures
            );

            Parts = _model.Parts;
            Animator = new Animator(this);
            foreach (var animName in animationNames)
            {
                var animation = LoadAnimation(animationsPath, animName);
                Animator.AddAnimation(animation);

            }
            Animator.SelectAnimation(animationNames[0]);

        }
        public Matrix4x4[] GetAnimationBoneMatrices()
        {
            return Animator.FinalBoneMatrices;
        }
        public void SelectAnimation(string name)
        {
            Animator.SelectAnimation(name);
        }
        public void Draw()
        {
            _model.Draw();
        }
        public void Update(float deltaTime)
        {
            Animator.Update(deltaTime);
        }

        public void Blend(string a1, string a2, float ammount, float deltaTime)
        {
            Animator.BlendBetween(a1, a2, ammount, deltaTime);
        }
        public void Dispose()
        {
            _model.Dispose();
        }

        public Model GetBaseModel()
        {
            return _model;
        }

        private unsafe Animation LoadAnimation(string path, string name)
        {
            var scene = _assimp.ImportFile(path + name, (uint)(0));

            var sceneNull = scene == null;
            var sceneNullRootNode = scene == null;
            var rootNode = scene->MRootNode;
            if (scene == null || rootNode == null)
            {
                var error = _assimp.GetErrorStringS();
                throw new Exception(error);
            }
            if (!_modelHierarchySet)
            {
                var globalTransform = Matrix4x4.Transpose(rootNode->MTransformation);
                Animator.InverseGlobalTransform = Matrix4x4.Invert(globalTransform, out var inverse) ? inverse : Matrix4x4.Identity;

                //Log.Debug("-------------");
                //Log.Debug("HIERACHY");
                //_nCount = 0;
                //PrintHierarchy(rootNode, 0);

                //ReadHierarchy(rootNode, -1, 0);
                var rootFolded = ReadHierarchy(rootNode);
                //Log.Debug("-------------");
                //Log.Debug("FOLDED HIERACHY");
                //_nCount = 0;
                //PrintFoldedHierachy(_rootBoneHierarchyNode, -1);
                //Log.Debug("-------------");
                //Log.Debug("FLATTENED");
                FlattenHierarchy(rootFolded, -1);
                AnimatorNodes = _animatorNodes.ToArray();
                //PrintFlattened();

                _modelHierarchySet = true;
            }

            return new Animation(name, scene, _model);
        }

        private unsafe void FlattenHierarchy(ModelBoneHierarchyNode node, int parentID, int level = -1)
        {
            int currentIndex = _animatorNodes.Count; // index that will be assigned to this node in the flat array

            if (node.IsBone)
            {
                var boneInfoMap = _model.BoneInfoMap;
                if (boneInfoMap.TryGetValue(node.Name, out var info))
                {
                    var an = new AnimatorNode(node.Transform, parentID, info.ID, info.Offset);
                    an.Name = node.Name.TrimBoneName();
                    //an.Level = level;
                    _animatorNodes.Add(an);
                }
            }
            else
            {
                var an = new AnimatorNode(node.Transform, parentID);
                an.Name = node.Name.TrimBoneName();
                //an.Level = level;
                _animatorNodes.Add(an);
            }

            foreach (var child in node.Children)
            {
                FlattenHierarchy(child, currentIndex, level + 1);
            }
        }

        private unsafe ModelBoneHierarchyNode ReadHierarchy(Node* node)
        {
            // Try to detect if this node is the head of a helper-chain that ends in a bone node.
            // (Mixamo pre-scale_bone, pre-rotation_bone, pre-translation_bone, bone, style)
            // If it is, fold the transforms along the single-child chain up to the bone and
            // create a single AnimationNode entry for that bone (with BaseTransform = folded transform).

            var boneInfoMap = _model.BoneInfoMap;
            // Attempt to collect a chain starting at 'node' that leads to a bone node
            if (TryCollectChainThatEndsInBone(node, boneInfoMap, out var foldedTransform, out Node* boneNode, out string boneName))
            {
                // We found a chain that ends in a bone node (boneNode). Add a single AnimationNode
                // using the folded transform and skip the intermediate helper nodes in the
                // flattened AnimationNodes list (but still recurse children of the bone node).

                if (boneInfoMap.TryGetValue(boneName, out var info))
                {
                    var children = new List<ModelBoneHierarchyNode>();
                    for (int i = 0; i < boneNode->MNumChildren; i++)
                    {
                        var child = ReadHierarchy(boneNode->MChildren[i]);
                        if (child != null)
                            children.Add(child);
                    }
                    return new ModelBoneHierarchyNode(boneName, foldedTransform, children, info.Offset);
                }
            }

            // Normal path: this node is not the head of helper chain ending in a bone.
            string name = node->MName;

            var nodeTransform = node->MTransformation;
            var nodeChildren = new List<ModelBoneHierarchyNode>();

            for (var i = 0; i < node->MNumChildren; i++)
            {
                var child = ReadHierarchy(node->MChildren[i]);
                if (child != null)
                    nodeChildren.Add(child);
            }

            if (boneInfoMap.TryGetValue(name, out var directInfo))
            {
                return new ModelBoneHierarchyNode(name, nodeTransform, nodeChildren, directInfo.Offset);
            }
            else
            {
                if (nodeChildren.Count == 0) // skip non-bone with no children
                    return null;

                return new ModelBoneHierarchyNode(name, nodeTransform, nodeChildren);
            }
        }

        // Helper: tries to walk a single-child chain starting at 'start' and sees if it ends in a bone node.
        // It accumulates each node's transform into 'accumulated' (in System.Numerics row-major space using Transpose).
        private unsafe bool TryCollectChainThatEndsInBone(Node* start, Dictionary<string, BoneInfo> boneInfoMap, out Matrix4x4 accumulated, out Node* boneNodeOut, out string boneNameOut)
        {
            accumulated = Matrix4x4.Identity;
            Node* cur = start;
            boneNodeOut = null;
            boneNameOut = null;

            // Walk while there's exactly one child (linear chain) and stop if we find a bone node
            while (cur != null)
            {
                var t = cur->MTransformation;
                accumulated = accumulated * t;
                string curName = cur->MName;

                // If this node maps directly to a bone, we finished the chain
                if (boneInfoMap.ContainsKey(curName))
                {
                    boneNodeOut = cur;
                    boneNameOut = curName;
                    return true;
                }

                // if there's not exactly one child, we can't continue folding safely
                if (cur->MNumChildren != 1)
                {
                    boneNodeOut = cur; // caller may still recurse from here
                    return false;
                }

                // Step into the single child
                cur = cur->MChildren[0];
            }
            return false;
        }


        int _nCount = 0;
        private unsafe void PrintHierarchy(Node* node, int level)
        {
            var str = $"{_nCount} ";
            for (var i = 0; i < level; i++)
            {
                str += "-";
            }
            str += ((string)node->MName).TrimBoneName();
            Log.Debug(str);
            _nCount++;

            for (var i = 0; i < node->MNumChildren; i++)
            {
                PrintHierarchy(node->MChildren[i], level + 1);
            }
        }
        private int TabCount(int pid)
        {
            if (pid == -1 || pid == 0)
                return 0;

            return TabCount(_animatorNodes[pid].ParentID) + 1;

        }
        
        private unsafe void PrintFlattened()
        {
            Log.Debug("[Animator Nodes] AM");
            for (var i = 0; i < _animatorNodes.Count; i++)
            {
                var node = _animatorNodes[i];
                var str = node.IsBone ? "B" : "N";
                var spc = "";
                for (var j = 0; j < TabCount(node.ParentID); j++)
                {
                    spc += "-";
                }
                str += $"{i}{spc} PID {node.ParentID}, MID {node.ModelBoneID}, {node.Name}";
                Log.Debug(str);
            }
            Log.Debug("---");
        }
        private unsafe void PrintFoldedHierachy(ModelBoneHierarchyNode node, int parentID)
        {

            var str = $"{_nCount}";

            str += node.IsBone ? "B" : " ";

            for (var i = 0; i < parentID; i++)
            {
                str += "-";
            }
            str += $"PID {parentID} " + (node.Name).TrimBoneName();
            Log.Debug(str);
            for (var i = 0; i < node.Children.Count; i++)
            {
                _nCount++;
                PrintFoldedHierachy(node.Children[i], parentID + 1);
            }
        }
    }
}
