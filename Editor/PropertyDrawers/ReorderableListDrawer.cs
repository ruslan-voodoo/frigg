﻿namespace Frigg.Editor {
    using System;
    using System.Collections;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;
    using Utils;

    public class ReorderableListDrawer : FriggPropertyDrawer {
        private         ReorderableList list;

        public override float GetHeight() {
            //header
            var total = 0f;
            if (this.list.displayAdd || this.list.displayRemove) {
                //button
                total += EditorGUIUtility.singleLineHeight * 2;
            }

            this.property.IsExpanded =  true;
            total                    += FriggProperty.GetPropertyHeight(this.property) + 4f * this.list.list.Count; // paddings
            return total;
        }

        public override bool  IsVisible => true;

        public ReorderableListDrawer(FriggProperty prop) : base(prop) {
        }

        public override void Draw(Rect rect) {
            var elements = (IList) CoreUtilities.GetTargetObject(this.property.ParentValue, this.PropertyMeta.MemberInfo);
            if(this.list == null)
                this.list = new ReorderableList(elements, CoreUtilities.TryGetListElementType(elements.GetType()));
            
            this.SetCallbacks(this.list, this.property);

            rect.width -= EditorGUI.indentLevel * 15;
            rect.x     += EditorGUI.indentLevel * 15;
            
            this.list.DoList(rect);
        }
        
        public override void DrawLayout() {
            var elements = (IList) CoreUtilities.GetTargetObject(this.property.ParentValue, this.PropertyMeta.MemberInfo);
            
            if(this.list == null)
               this.list = new ReorderableList(elements, CoreUtilities.TryGetListElementType(elements.GetType()));
            
            this.SetCallbacks(this.list, this.property);
            this.list.DoLayoutList();
        }

        private void SetCallbacks(ReorderableList reorderableList, FriggProperty prop) {
            this.list = reorderableList;

            var hideHeader                   = false;
            this.list.draggable = this.list.displayAdd = this.list.displayRemove = true;
            
            var attr                         = prop.TryGetFixedAttribute<ListDrawerSettingsAttribute>();
            if (attr != null) {
                hideHeader      = attr.HideHeader;
                this.list.draggable  = attr.AllowDrag;
                this.list.displayAdd = !attr.HideAddButton;
                this.list.displayRemove = !attr.HideRemoveButton;
            }

            if (!hideHeader) {
                this.list.drawHeaderCallback = tempRect => {
                    EditorGUI.LabelField(tempRect,
                        new GUIContent($"{this.property.NiceName} - {this.list.count} elements"));
                };
            }

            this.list.drawElementCallback = (tempRect, index, active, focused) => {
                var pr = prop.GetArrayElementAtIndex(index);

                tempRect.y      += GuiUtilities.SPACE / 2f;
                tempRect.height =  EditorGUIUtility.singleLineHeight;
                tempRect.width  += EditorGUI.indentLevel * 15;
                tempRect.x      -= EditorGUI.indentLevel * 15;
                
                EditorGUI.BeginChangeCheck();
                pr.Draw(tempRect);
                if (EditorGUI.EndChangeCheck()) {
                    CoreUtilities.OnValueChanged(pr);
                }
            };

            var listType = CoreUtilities.TryGetListElementType(this.list.list.GetType());

            this.list.onAddCallback = delegate {
                var copy = this.list.list;

                this.list.list = Array.CreateInstance(listType, copy.Count + 1);
                for (var i = 0; i < copy.Count; i++) {
                    this.list.list[i] = copy[i];
                }
            };

            this.list.onRemoveCallback = delegate {
                var copy      = this.list.list;
                var newLength = copy.Count - 1;

                this.list.list = Array.CreateInstance(listType, newLength);
                for (var i = 0; i < this.list.index; i++)
                    this.list.list[i] = copy[i];

                for (var i = this.list.index; i < newLength; i++)
                    this.list.list[i] = copy[i + 1];
            };

            this.list.elementHeightCallback = index => {
                var element = this.property.GetArrayElementAtIndex(index);
                var height  = FriggProperty.GetPropertyHeight(element);
                return height + GuiUtilities.SPACE;
            };
        }
    }
}