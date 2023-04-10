using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using JSON = System.Collections.Generic.Dictionary<string, object>;

namespace GameObjectClasses
{  
    public class Factory<TObjectType, TParam1>
    {   
        public Factory(Type type)
        {
            if (!typeof(TObjectType).IsAssignableFrom(type))
                throw new System.ArgumentException(" type must be inherited from " + typeof(TObjectType));

            CreateTree(type);
        }

        ParameterExpression _constructorParam;
        NewExpression _callConstructor;
        Func<TParam1, TObjectType> _factoryMethod;
        private void CreateTree(Type type)
        {
            var constr = type.GetConstructor(new Type[] { typeof(TParam1) });
            if (constr == null)
                throw new ArgumentException("object without constructor " + type);//System.Exception("BackComponent component without constructor " + key);


            _constructorParam = Expression.Parameter(typeof(TParam1), "data");
            _callConstructor  = Expression.New(constr, _constructorParam);

            _factoryMethod    = Expression.Lambda<Func<TParam1, TObjectType>>(_callConstructor, _constructorParam).Compile();
        }

        internal TObjectType Build(TParam1 data)
        {
            var newObject = _factoryMethod(data);
            return newObject;
        }
    }
}
