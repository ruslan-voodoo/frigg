﻿namespace Frigg.Editor {
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEditor.Graphs;
    using UnityEditorInternal;
    using UnityEngine;
    using Utils;

    public class ReorderableListDrawer : CustomPropertyDrawer {
        public static ReorderableListDrawer instance = new ReorderableListDrawer();

        private Dictionary<string, ReorderableList> reorderableLists = new Dictionary<string, ReorderableList>();

        public static void ClearData() {
            instance = new ReorderableListDrawer();
        }

        private string GetPropertyKey(SerializedProperty property) => 
            property.serializedObject.targetObject.GetInstanceID() + "." + property.name;

        protected override void CreateAndDraw(Rect rect, SerializedProperty property, GUIContent label) {
            var name = property.propertyPath.Split('.');
            var p    = property.serializedObject.FindProperty(name[0]);

            if(!p.isArray)
                return;
 
            ReorderableList reorderableList;

            if (!instance.reorderableLists.ContainsKey(p.name)) {
                var attr = CoreUtilities.TryGetAttribute<ListDrawerSettingsAttribute>(property);
                
                if (attr != null) {
                    reorderableList = new ReorderableList(p.serializedObject, p,
                        attr.AllowDrag, !attr.HideHeader, !attr.HideAddButton, !attr.HideRemoveButton);
                    SetCallbacks(property, reorderableList, attr.HideHeader);
                }
                else {
                    reorderableList = new ReorderableList(p.serializedObject, p,
                        true, true, true, true);
                    SetCallbacks(property, reorderableList);
                }

                instance.reorderableLists[p.name] = reorderableList;
            }
            
            //if is null
            reorderableList = instance.reorderableLists[p.name];
            reorderableList.DoLayoutList();
        }

        private static void SetCallbacks(SerializedProperty property, ReorderableList reorderableList, bool hideHeader = false) {

            if (!hideHeader) {
                reorderableList.drawHeaderCallback = tempRect => {
                    EditorGUI.LabelField(tempRect,
                        new GUIContent($"{reorderableList.serializedProperty.displayName} - {reorderableList.count} elements"));
                };
            }

            reorderableList.drawElementCallback = (tempRect, index, active, focused) => {
                var element = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
                tempRect.y     += 2.0f;
                tempRect.x     += 10.0f;
                tempRect.width -= 10.0f;
                
                EditorGUI.PropertyField(new Rect(tempRect.x, tempRect.y, tempRect.width, 
                    EditorGUIUtility.singleLineHeight), element, true);
            };

            reorderableList.onChangedCallback = list => {
                
            };

            reorderableList.onAddCallback = delegate {
                property.arraySize++;

                var type = CoreUtilities.TryGetListElementType(CoreUtilities.GetPropertyType(property));

                var element = property.GetArrayElementAtIndex(property.arraySize - 1);

                CoreUtilities.SetDefaultValue(element, type);
            };

            reorderableList.onRemoveCallback = delegate {
                var size = property.arraySize;

                for (var i = reorderableList.index; i < size - 1; i++) {
                    reorderableList.serializedProperty.MoveArrayElement(i + 1, i);
                }

                property.arraySize--;
            };

            reorderableList.elementHeightCallback = index
                => EditorGUI.GetPropertyHeight(reorderableList.serializedProperty.GetArrayElementAtIndex(index)) + 5.0f;
        }
    }
}