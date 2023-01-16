using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
[CreateAssetMenu(fileName = "SpriteSlicer", menuName = "ScriptableObjects/Sprites/SpriteSlicer", order = 1)]
public class SpriteSlicer : ScriptableObject {
    public List<Texture2D> Textures;
}
[CustomEditor(typeof(SpriteSlicer))]
public class SpriteSlicerEditor : Editor {
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        if (GUILayout.Button("Slice")) {
            var list = (target as SpriteSlicer).Textures;
            foreach (var texture in list) {
                string path = AssetDatabase.GetAssetPath(texture);
                TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
                ti.isReadable = true;
 
                List<SpriteMetaData> newData = new List<SpriteMetaData>();
 
                int SliceWidth = 128;
                int SliceHeight = 128; 
 
                for (int i = 0; i < texture.width; i += SliceWidth)
                {
                    for(int j = texture.height; j > 0;  j -= SliceHeight)
                    {
                        SpriteMetaData smd = new SpriteMetaData();
                        smd.pivot = new Vector2(0.5f, 0.5f);
                        smd.alignment = 9;
                        smd.name = (texture.height - j)/SliceHeight + ", " + i/SliceWidth;
                        smd.rect = new Rect(i, j-SliceHeight, SliceWidth, SliceHeight);
 
                        newData.Add(smd);
                    }
                }
 
                ti.spritesheet = newData.ToArray();
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            
        }
    }
}