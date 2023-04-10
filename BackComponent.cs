using JSON = System.Collections.Generic.Dictionary<string, object>;
using Battle.Core;
using System.Reflection;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Battle.BackEnd
{

    public abstract partial class BackComponent: IBackComponent
    {
        public int Puid { get; private set; }
        protected JSON _initInfo { get; private set; }

        public BackComponent(JSON root)
        {
            _initInfo = root;
            Puid      = root.GetInt("_puid");

            _selfActive = !root.ContainsKey("_off");
        }

        public BackComponent(int puid)
        {
            Puid        = puid;
            _selfActive = true;
        }

        public BackendObject Owner { get; private set; }
        public void SetOwner(BackendObject owner)
        {
            Owner = owner;
            
            OwnerChanged();
        }

        protected virtual void OwnerChanged()
        {

        }

        internal void ProcessLinks()
        {
            CustomLinkProcess(_initInfo);
            _initInfo = null;
        }

        public ComponentType ComponentType
        {
            get
            {
                var type = GetType();
                if(s_typesTableTable.TryGetValue(type, out var typeID))
                {
                    return typeID;
                }

                UnityEngine.Debug.Log("Type: " + type + " " + s_typesTableTable.Count);
                throw new System.Exception("Component have not type id attribute");
            }
        }

        protected virtual void CustomLinkProcess(JSON root)
        {
            
        }

        bool ProcessArrayTypes(FieldInfo fieldInfo, string fieldPath, string value)
        {
            if (fieldPath.Contains("Array"))
            {
                fieldPath = fieldPath.Substring(fieldPath.IndexOf('.') + 1);
            }
            Type elementType = fieldInfo.FieldType.GetElementType();
            Array trgArray = (Array)fieldInfo.GetValue(this);
            if (fieldPath.Contains("size"))
            {
                int size = int.Parse(value);
                Array newArray = Array.CreateInstance(elementType, size);
                Array.ConstrainedCopy(trgArray, 0, newArray, 0, Mathf.Min(trgArray.Length, size));
                fieldInfo.SetValue(this, Convert.ChangeType(newArray, fieldInfo.FieldType));
            }

            if (fieldPath.Contains("data"))
            {
                var fIdx = fieldPath.IndexOf('[');
                string strIndex = fieldPath.Substring(fIdx + 1, fieldPath.IndexOf(']') - fIdx - 1);
                if (strIndex.Equals(""))
                    return false;

                var index = int.Parse(strIndex);
                if (fieldPath.Contains("."))
                {// works with complex array elements
                    var subFieldpath = fieldPath.Substring(fieldPath.IndexOf('.') + 1);
                    var item = trgArray.GetValue(index);
                    var itemType = item.GetType();
                    var field = itemType.GetField(subFieldpath, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                    if (field.FieldType.IsArray)
                    {
                        return ProcessArrayTypes(field, subFieldpath, value);
                    }

                    if (value.Equals("null"))
                        field.SetValue(item, Convert.ChangeType(null, field.FieldType));
                    else
                    {

                        SetValueToField(field, item, value);
                    }

                    trgArray.SetValue(Convert.ChangeType(item, elementType), index);
                }
                else
                    trgArray.SetValue(Convert.ChangeType(value, elementType), index);

                fieldInfo.SetValue(this, Convert.ChangeType(trgArray, fieldInfo.FieldType));
            }

            return true;
        }

        void SetValueToField(FieldInfo fi, object item, string value)
        {
            if (fi.FieldType.IsClass)
            {//process links
                EntityBack entity = null;
                string[] val = value.Split(':');
                int entIdx = -1;
                if (val.Length >= 2)
                {
                    entIdx = int.Parse(val[0]);
                    
                    if (entIdx >= 0)
                    {//Find entity
                        entity = Owner.OwnerEntity.BackLevel.GetEntity(entIdx);
                        if (entity == null)
                            Debug.LogException(new Exception("Entity not found " + entIdx));
                    }
                    else
                    {//local entity
                        entity = Owner.OwnerEntity;
                    }
                }
                var compIdx = int.Parse(val[1]);
                BackComponent target = null;
                if (compIdx > 0)
                { //find component
                    target = entity.GetComponentByPUID(compIdx);
                    if (target == null)
                        Debug.LogException(new Exception("Component PUID " + compIdx + " not found for entity " + entIdx));
                    fi.SetValue(item, target);
                }
                else
                    fi.SetValue(item, entity);
            }
            else
                fi.SetValue(item, Convert.ChangeType(value, fi.FieldType));
        }

        static FieldInfo SelectFieldByName(Type ownerType, string fieldPath, ref object target)
        {
            string subFieldpath = "";
            if (fieldPath.Contains("."))
            {
                subFieldpath = fieldPath.Substring(fieldPath.IndexOf('.') + 1);
                fieldPath = fieldPath.Substring(0, fieldPath.IndexOf('.'));
            }
            Type to = typeof(BackComponent);
            for (var t = ownerType; t != to; t = t.BaseType)
            {
                var tt = t.GetField(fieldPath, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (tt != null && !subFieldpath.Equals(""))
                {
                    var subType = tt.FieldType;
                    if (subType.IsArray)
                    {
                        return tt;
                    }
                    target = tt.GetValue(target);
                    tt = subType.GetField(subFieldpath, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                }
                if (tt != null)
                {                    
                    return tt; 
                }
            }
            target = null;
            return null;
        }

        internal virtual bool ApplyModiffication(string path, string value, Link link)
        {
            object trg = this;
            var prop = SelectFieldByName(GetType(), path, ref trg);
            if (prop == null)
            {
                UnityEngine.Debug.Log("There is no searched field " + path);
                return false;
            }
            if (prop.FieldType.IsArray)
            {
                var subFieldpath = path.Substring(path.IndexOf('.') + 1);
                return ProcessArrayTypes(prop, subFieldpath, value);
            }
            SetValueToField(prop, trg, value);

            return true;
        }

        
        protected T GetOtherComponent<T>(int instanceID, int componentID) 
            where T : BackComponent
        {
            EntityBack targetEntity;

            UnityEngine.Debug.Log(" GetOtherComponent " + instanceID + " - " + componentID);

            if (instanceID == 0 || instanceID == -1) ///тут надоопределиться с идексами
                targetEntity = Owner.OwnerEntity;
            else
                targetEntity = Owner.OwnerEntity.BackLevel.GetEntity(instanceID);

            if (targetEntity == null)
                return null;

            return targetEntity.GetComponentByPUID<T>(componentID);
        }


        internal void Start()
        {
            CustomStartAction();
        }

        protected virtual void CustomStartAction()
        {

        }

        public bool IsRemoved { get; private set; }
        public void Remove()
        {
            IsRemoved = true;
        }
        
        private bool _selfActive;
        public void Activate()
        {
            if (IsRemoved)
                throw new BackComponentException(this, "try activate removed component");// System.Exception("try activate removed component");

            if (_selfActive)
                return;
            _selfActive = true;

            

            CheckActivation();

            Owner.OwnerEntity.BackLevel.RegisterComponentActivation(this);
        }

        public void Deactivate()
        {
            if (!_selfActive)
                return;
            _selfActive = false;

            CheckActivation();
        }


        private bool _ownerActivated;
        public void OwnerActivated()
        {
            if (_ownerActivated)
                return;
            _ownerActivated = true;

            CheckActivation();
        }

        public void OwnerDeactivated()
        {
            if (!_ownerActivated)
                return;
            _ownerActivated = false;

            CheckActivation();
        }

        public bool IsActive { get; private set; }
        void CheckActivation()
        {
            var newState = _ownerActivated && _selfActive;
            
            if(newState == IsActive)
                return;

            IsActive = newState;

            if(IsActive)
                CustomActivationActions();
            else
                CustomDeactivationActions();
        }

        protected virtual void CustomActivationActions()
        {

        }

        

        protected virtual void CustomDeactivationActions()
        {

        }

        public virtual void Update()
        {
            
        }

        public virtual void Pause()
        {
            Paused = true;
        }

        public virtual void UnPause()
        {
            Paused = false;
        }

        public bool Paused { get; set; }

        public void Action()
        {
            if (IsRemoved)
                throw new BackComponentException(this, "try activate removed component"); //System.Exception("try activate removed component");

            if (!IsActive)
                return;

            ActionImplementation();


            Owner.OwnerEntity.BackLevel.RegisterComponentEvent(this);
        }

        protected virtual void ActionImplementation()
        {

        }

        

        
    }
}