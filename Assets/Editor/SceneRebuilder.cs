// Script để Rebuild lại Scene tự động khi vào Editor
using UnityEditor;
using UnityEngine;

namespace TinyToysFactory.Editor
{
    public class SceneRebuilder
    {
        [MenuItem("TinyToysFactory/Auto Rebuild Scene")]
        public static void Rebuild()
        {
            TinyToysSceneBuilder.BuildScene();
            Debug.Log("Scene rebuilt with new UI coords.");
        }
    }
}
