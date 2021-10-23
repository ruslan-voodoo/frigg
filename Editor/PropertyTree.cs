﻿namespace Frigg.Editor {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Groups;
    using Layouts;
    using Packages.Frigg.Editor.Utils;
    using UnityEditor;
    using UnityEngine;
    using Utils;

    public abstract class PropertyTree {
        public abstract SerializedObject            SerializedObject { get; }

        public abstract IEnumerable<FriggProperty> EnumerateTree(bool includeChildren);

        public List<Layout> Layouts = new List<Layout>();

        public abstract Type             TargetType       { get; }

        public abstract FriggProperty RootProperty { get; set; }

        public abstract void UpdateTree();

        public static PropertyTree InitTree(object target) {
            if (target == null)
                throw new ArgumentNullException("target");

            return InitTree(new[] {target}, null);
        }
        
        public static PropertyTree InitTree(params object[] targets) {
            if (targets == null)
                throw new ArgumentNullException("targets");

            return InitTree((IList) targets);
        }
        
        public static PropertyTree InitTree(IList targets) {
            if (targets == null)
                throw new ArgumentNullException("targets");

            return InitTree(targets, null);
        }
        
        public static PropertyTree InitTree(SerializedObject target) {
            if (target == null)
                throw new ArgumentNullException("target");

            return InitTree(target.targetObjects, target);
        }

        private static PropertyTree InitTree(IList targets, SerializedObject serializedObject) {
            if (targets == null)
            {
                throw new ArgumentNullException("targets");
            }

            if (targets.Count == 0)
            {
                throw new ArgumentException("There must be at least one target.");
            }

            if (serializedObject != null)
            {
                var valid = true;
                var targetObjects = serializedObject.targetObjects;

                if (targets.Count != targetObjects.Length)
                {
                    valid = false;
                }
                else
                {
                    for (var i = 0; i < targets.Count; i++) {
                        if (ReferenceEquals(targets[i], targetObjects[i])) {
                            continue;
                        }

                        valid = false;
                        break;
                    }
                }

                if (!valid)
                {
                    throw new ArgumentException("Given target array must be identical in length and content to the target objects array in the given serializedObject.");
                }
            }

            Type targetType = null;

            for (var i = 0; i < targets.Count; i++)
            {
                Type otherType;
                var target = targets[i];

                if (ReferenceEquals(target, null))
                {
                    throw new ArgumentException("Target at index " + i + " was null.");
                }

                if (i == 0)
                {
                    targetType = target.GetType();
                }
                else if (targetType != (otherType = target.GetType()))
                {
                    if (targetType.IsAssignableFrom(otherType))
                    {
                        continue;
                    }

                    if (!otherType.IsAssignableFrom(targetType)) {
                        throw new ArgumentException("Expected targets of type " + targetType.Name + ", but got an incompatible target of type " + otherType.Name + " at index " + i + ".");
                    }

                    targetType = otherType;
                }
            }

            var treeType = typeof(PropertyTree<>).MakeGenericType(targetType);
            Array targetArray;

            if (targets.GetType().IsArray && targets.GetType().GetElementType() == targetType)
            {
                targetArray = (Array)targets;
            }
            else
            {
                targetArray = Array.CreateInstance(targetType, targets.Count);
                targets.CopyTo(targetArray, 0);
            }

            if (serializedObject == null && targetType.IsAssignableFrom(typeof(UnityEngine.Object)))
            {
                var objs = new UnityEngine.Object[targets.Count];
                targets.CopyTo(objs, 0);

                serializedObject = new SerializedObject(objs);
            }

            return (PropertyTree) Activator.CreateInstance(treeType, targetArray, serializedObject);
        }

        public abstract void Draw();
    }

    public class PropertyTree<T> : PropertyTree {
        private SerializedObject serializedObject;

        private T[] memberTargets;

        public sealed override FriggProperty RootProperty { get; set; }

        public List<T> Targets => this.memberTargets.ToList();
        
        private static bool isValueType   = typeof(T).IsValueType;
        private static bool isUnityObject = typeof(UnityEngine.Object).IsAssignableFrom(typeof(T));

        public override        SerializedObject SerializedObject => this.serializedObject;
        public sealed override Type             TargetType       => typeof(T);

        public PropertyTree(SerializedObject serializedObject) : this(serializedObject.targetObjects.Cast<T>().ToArray(), serializedObject) {
        }

        public PropertyTree(T[] targets) : this(targets, null) {
        }

        public PropertyTree(T[] targetObjects, SerializedObject serializedObject) {
            this.serializedObject = serializedObject;
            this.memberTargets    = targetObjects;

            var metaInfo = new PropertyMeta {
                Name       = this.memberTargets[0].GetType().Name,
                MemberType = this.TargetType,
                MemberInfo = this.memberTargets[0].GetType()
            };

            this.RootProperty = FriggProperty.DoProperty(
                this,
                new FriggProperty(new PropertyValue<object>(this.memberTargets[0], 
                    this.memberTargets[0], metaInfo)), metaInfo, true);

        }

        //Draw section. 
        public override void Draw() {
            this.BeginDrawTree();
            this.DrawTree();
            this.EndDrawTree();
        }
        
        //We need to call this method only when we are updating our properties (trying to get new values).
        //Cycle should look like:
        //Get all properties -> Draw them -> Get incoming changes
        //-> Call Update method which should refresh root property and register objects manually.
        public override void UpdateTree() {
            foreach (var prop in this.EnumerateTree(true)) {
                prop.Refresh();
            }
            
            this.SerializedObject.ApplyModifiedProperties(); 
        }
        
        private void BeginDrawTree() {
            this.SerializedObject.Update();
        }

        private void DrawTree() {
            foreach (var prop in this.EnumerateTree(false)) {
                if(!prop.IsLayoutMember)
                   prop.Draw();
            }
        }

        private void EndDrawTree() {
            this.SerializedObject.ApplyModifiedProperties();
        }

        public override IEnumerable<FriggProperty> EnumerateTree(bool includeChildren)
        {
            for (var i = 0; i < this.RootProperty.ChildrenProperties.AmountOfChildren; i++)
            {
                var prop = this.RootProperty.ChildrenProperties[i];

                yield return prop;

                if (!includeChildren) {
                    continue;
                }

                foreach (var child in prop.ChildrenProperties.RecurseChildren())
                {
                    yield return child;
                }
            }
        }
    }
}