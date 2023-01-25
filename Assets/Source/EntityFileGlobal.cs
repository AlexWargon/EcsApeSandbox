using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Wargon.Ecsape;

[InitializeOnLoad]
 public class EntityFileGlobal
 {
     private static EntityPrefab wrapper = null;
     private static bool selectionChanged = false;
 
     static EntityFileGlobal()
     {
         Selection.selectionChanged += SelectionChanged;
         EditorApplication.update += Update;
     }
 
     private static void SelectionChanged()
     {
         selectionChanged = true;
     }
 
     private static void Update()
     {
         if (selectionChanged == false) return;
 
         selectionChanged = false;
         if (Selection.activeObject != wrapper)
         {
             string fn = AssetDatabase.GetAssetPath(Selection.activeInstanceID);
             if (fn.ToLower().EndsWith(".bytes"))
             {
                 if (wrapper == null)
                 {
                     wrapper = ScriptableObject.CreateInstance<EntityPrefab>();
                     wrapper.hideFlags = HideFlags.DontSave;
                 }
 
                 wrapper.filePath = fn;
                 wrapper.fileName = Path.GetFileName(fn).Replace(".bytes", "");
                 Selection.activeObject = wrapper;
 
                 Editor[] ed = Resources.FindObjectsOfTypeAll<EntityFileWrapperInspector>();
                 if (ed.Length > 0) ed[0].Repaint();
             }
         }
     }
 }
 
 // IniFileWrapper.cs 
 public class EntityPrefab: ScriptableObject
 {
     [NonSerialized] public string filePath; // path is relative to Assets/
     [NonSerialized] public string fileName;
 }
 
 // IniFileWrapperInspector.cs
 [CustomEditor(typeof(EntityPrefab))]
 public class EntityFileWrapperInspector: Editor {
     private static bool dirty;
     private bool opened;
     private static EntityPrefabData currentFile;
     private static EntityPrefabData copy;
     private static string currentFilePath;
     private static readonly Dictionary<string, IComponentInspector> _inspectors 
         = new Dictionary<string, IComponentInspector>();

     private void OnEnable() {
         EntityPrefab Target = (EntityPrefab)target;
         currentFile = openFile(Target.filePath);
         copy = DeepClone(currentFile);
     }

     static EntityFileWrapperInspector() {
         _inspectors.Add(nameof(TestData1), new TestData1Inspector());
         _inspectors.Add(nameof(TestData2), new TestData2Inspector());

         //_fieldInspectors.Add(typeof(int), new IntInspector());
     }

     private static T DeepClone<T>(T obj)
     {
         using (var ms = new MemoryStream())
         {
             var formatter = new BinaryFormatter();
             formatter.Serialize(ms, obj);
             ms.Position = 0;
             return (T) formatter.Deserialize(ms);
         }
     }


     public override void OnInspectorGUI()
     {
         EntityPrefab Target = (EntityPrefab)target;
 
         GUILayout.Label("Editing: " + Target.fileName);
         
         if(Target.filePath==null) return;
         currentFilePath = Target.filePath;
         Draw(currentFilePath);
     }

     private static bool IsDirty() {
         return copy != currentFile;
     }
     void Draw(string path) {
         if (GUILayout.Button("Save")) {
             var newSave = new EntityPrefabData();
             newSave.Components.Add(new TestData1{ValueInt = 116661});
             newSave.Components.Add(new TestData2{ValueFloat = 666.666f});
             var dataStream = new FileStream(path, FileMode.OpenOrCreate);
             var converter = new BinaryFormatter();
             converter.Serialize(dataStream, newSave);
             dataStream.Close();
             dirty = true;
         }

         if (dirty) {
             currentFile = openFile(path);
             dirty = false;
         }

         for (var index = 0; index < currentFile.Components.Count; index++) {
             var component = currentFile.Components[index];
             currentFile.Components[index] = DrawData(component);
         }
         
         SaveFile();
     }
     private static void SaveFile() {
         var dataStream = new FileStream(currentFilePath, FileMode.OpenOrCreate);
         var converter = new BinaryFormatter();
         converter.Serialize(dataStream, currentFile);
         dataStream.Close();
         Debug.Log("SAVE");
     }
     static EntityPrefabData openFile(string path) {
         var dataStream = new FileStream(path, FileMode.Open);
         var converter = new BinaryFormatter();
         var saveData = converter.Deserialize(dataStream) as EntityPrefabData;
         dataStream.Close();
         return saveData;
     }
     object DrawData(object data) {
         return _inspectors[data.GetType().Name].Draw(data);
     }

     static EntityPrefabData Deserialize(TextAsset asset) {
         var converter = new BinaryFormatter();
         using var ms = new MemoryStream(asset.bytes);
         return converter.Deserialize(ms) as EntityPrefabData;
     }

     public static void AddComponents(ref Entity entity,TextAsset asset) {
         AddComponents(ref entity,Deserialize(asset));
     }

     static void AddComponents(ref Entity entity, EntityPrefabData data) {
         entity.Add((TestData1)data.Components[0]);
     }

}

namespace Wargon.Ecsape {
    public interface IComponentInspector {
        object Draw(object data);
    }

    public abstract class ComponentInspector<T> : IComponentInspector where T: struct, IComponent {
        protected abstract T DrawGeneric(T data);
        public object Draw(object data) {
            return DrawGeneric((T)data);
        }
    }
    public class TestData1Inspector : ComponentInspector<TestData1> {
        protected override TestData1 DrawGeneric(TestData1 data) {
            data.ValueInt = EditorGUILayout.IntField(nameof(data.ValueInt), data.ValueInt);
            return data;
        }
    }
    public class TestData2Inspector : ComponentInspector<TestData2> {
        protected override TestData2 DrawGeneric(TestData2 data) {
            data.ValueFloat = EditorGUILayout.FloatField(nameof(data.ValueFloat), data.ValueFloat);
            return data;
        }
    }
}

public interface FieldInspector {
    object Draw(object field, VisualElement root);
}

public abstract class FieldInspector<T> : FieldInspector {
    public object Draw(object field, VisualElement root) {
        return Draw((int)field, root);
    }

    protected abstract object Draw(int field, VisualElement root);
}

[Serializable]
public struct TestData1 : IComponent {
    public int ValueInt;
}
[Serializable]
public struct TestData2 : IComponent {
    public float ValueFloat;
}
[Serializable]
public class EntityPrefabData {
    public List<object> Components = new List<object>();
}
[Serializable]
public class ComponentData {
    public List<FieldData> Fields = new List<FieldData>();
}
[Serializable]
public class FieldData {
    public string Name;
    public string Type;
    public string Data;
}