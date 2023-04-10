using System;
using System.Collections.Generic;
using System.Reflection;
using JSON = System.Collections.Generic.Dictionary<string, object>;
using Battle.Core;

namespace Battle.BackEnd
{

    public abstract partial class BackComponent
    {

        static Dictionary<ComponentType, ConstructorInfo> constructTable;
        static Dictionary<Type, ComponentType> s_typesTableTable;

        static void Init()
        {
            constructTable = new Dictionary<ComponentType, ConstructorInfo>();
            s_typesTableTable = new Dictionary<Type, ComponentType>();

            //var assemble = AppDomain.CurrentDomain.GetAssemblies();
            Type[] types = Assembly.GetExecutingAssembly().GetTypes();
            var ti = typeof(BackComponent);
            foreach (Type t in types)
            {
                if (ti.IsAssignableFrom(t))
                {
                    var attr = t.GetCustomAttributes(typeof(ComponentTypeAttribute), false);
                    if (attr == null || attr.Length == 0)
                    {
                        UnityEngine.Debug.Log("BackComponent component without ComponentTypeAttribute: " + t);
                        continue;
                    }
                    if (t.IsAbstract)
                    {
                        UnityEngine.Debug.Log("ABSTRACT BackComponent component with ComponentTypeAttribute: " + t);
                        continue;
                    }

                    var key = ((ComponentTypeAttribute) attr[0]).Type;
                    var constr = t.GetConstructor(new Type[] { typeof(JSON) });
                    if (constr == null)
                    {
#if UNITY_EDITOR
                        throw new BackComponentException("BackComponent component without constructor " + key);//System.Exception("BackComponent component without constructor " + key);
#endif
                        UnityEngine.Debug.Log("BackComponent component without constructor " + key);
                    }
                    if (!constructTable.ContainsKey(key))
                    {
                        constructTable.Add(key, constr);
                        s_typesTableTable.Add(t, key);
                    }
                    else
                    {
                        UnityEngine.Debug.Log("BackComponent component with the same attrbute was added to table " + key);
                    }
                }
            }
        }


        public static Type GetComponentTypeByAttribute(ComponentType componentID)
        {
            if (constructTable == null)
            {
                Init();
            }
            if (!constructTable.ContainsKey(componentID))
            {
                throw new BackComponentException("Unknown component " + componentID);
            }
            
            return constructTable[componentID].ReflectedType;
        }

        static Type _lastComponentType;

        public static Type GetLastComponentType()
        {
            return _lastComponentType;
        }

        public static BackComponent CreateComponent(ComponentType componentID, JSON root)
        {
            if (!constructTable.ContainsKey(componentID))
            {
                throw new BackComponentException("Unknown component " + componentID);
            }

            return (BackComponent)constructTable[componentID].Invoke(new object[] { root });
        }

        public static BackComponent Parse(JSON root)
        {
            if (constructTable == null)
            {
                Init();
            }

            var componentID = (ComponentType) root.GetInt("_type");

            if (!constructTable.ContainsKey(componentID))
            {
                throw new BackComponentException("Unknown component " + componentID);
            }

            BackComponent cmp = (BackComponent)constructTable[componentID].Invoke(new object[] { root });

            _lastComponentType = cmp.GetType();
            return cmp;
        }
    }
}