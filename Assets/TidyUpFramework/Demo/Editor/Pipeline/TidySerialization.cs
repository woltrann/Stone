using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidySerialization
    {
        public static void Set(Object target, string propertyPath, Object value)
        {
            var serialized = new SerializedObject(target);
            var property = Require(serialized, propertyPath, target);
            if (property == null)
            {
                return;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetString(Object target, string propertyPath, string value)
        {
            var serialized = new SerializedObject(target);
            var property = Require(serialized, propertyPath, target);
            if (property == null)
            {
                return;
            }

            property.stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetFloat(Object target, string propertyPath, float value)
        {
            var serialized = new SerializedObject(target);
            var property = Require(serialized, propertyPath, target);
            if (property == null)
            {
                return;
            }

            property.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetInt(Object target, string propertyPath, int value)
        {
            var serialized = new SerializedObject(target);
            var property = Require(serialized, propertyPath, target);
            if (property == null)
            {
                return;
            }

            if (property.propertyType == SerializedPropertyType.Enum)
            {
                property.enumValueIndex = value;
            }
            else
            {
                property.intValue = value;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetBool(Object target, string propertyPath, bool value)
        {
            var serialized = new SerializedObject(target);
            var property = Require(serialized, propertyPath, target);
            if (property == null)
            {
                return;
            }

            property.boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetColor(Object target, string propertyPath, Color value)
        {
            var serialized = new SerializedObject(target);
            var property = Require(serialized, propertyPath, target);
            if (property == null)
            {
                return;
            }

            property.colorValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetVector3(Object target, string propertyPath, Vector3 value)
        {
            var serialized = new SerializedObject(target);
            var property = Require(serialized, propertyPath, target);
            if (property == null)
            {
                return;
            }

            property.vector3Value = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetArray<T>(Object target, string propertyPath, IReadOnlyList<T> values) where T : Object
        {
            var serialized = new SerializedObject(target);
            var property = Require(serialized, propertyPath, target);
            if (property == null)
            {
                return;
            }

            property.arraySize = values.Count;
            for (var index = 0; index < values.Count; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static SerializedProperty Require(SerializedObject serialized, string propertyPath, Object target)
        {
            var property = serialized.FindProperty(propertyPath);
            if (property == null)
            {
                Debug.LogError("Tidy Up pipeline: property '" + propertyPath + "' not found on " + target.GetType().Name);
            }

            return property;
        }
    }
}
