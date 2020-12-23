using System;
using ThreeRingsSharp.XansData.Structs;

namespace SKAnimatorTools.aytimothy {
    [Serializable]
    public class ExportedScene {
        public string name;
        public ExportedSceneObject[] objects;

        [Serializable]
        public class ExportedSceneObject {
            public string name;
            public Vector3 position;
            public Vector3 rotation;
            public Vector3 scale;
        }
    }
}
