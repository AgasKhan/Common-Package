using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class ReflectionExtension
{
    public static Type GetMemberType(this MemberInfo memberInfo)
    {
        switch (memberInfo.MemberType)
        {
            case MemberTypes.Field:
                return ((FieldInfo)memberInfo).FieldType;
                
            case MemberTypes.Method:
                return ((MethodInfo)memberInfo).ReturnType;
                
            case MemberTypes.Property:
                return ((PropertyInfo)memberInfo).PropertyType;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    
    public static object GetValue(this MemberInfo memberInfo, object Object, params object[] paramIndex)
    {
        switch (memberInfo.MemberType)
        {
            case MemberTypes.Field:
                return ((FieldInfo)memberInfo).GetValue(Object);
                
            case MemberTypes.Property:
                return  ((PropertyInfo)memberInfo).GetValue(Object, paramIndex);

            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    public static void SetValue(this MemberInfo memberInfo, object Object, object value,params object[] paramIndex)
    {
        switch (memberInfo.MemberType)
        {
            case MemberTypes.Field:
                ((FieldInfo)memberInfo).SetValue(Object, value);
                break;
                
            case MemberTypes.Property:
                ((PropertyInfo)memberInfo).SetValue(Object, value,paramIndex);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    public static IEnumerable<PropertyInfo> ToPropertyInfos(this IEnumerable<MemberInfo> members)
    {
        return members.Where(infos => infos.MemberType == MemberTypes.Property)
            .Cast<PropertyInfo>();
    }
    
    public static IEnumerable<FieldInfo> ToFieldInfos(this IEnumerable<MemberInfo> members)
    {
        return members.Where(infos => infos.MemberType == MemberTypes.Field)
            .Cast<FieldInfo>();
    }
    
    public static IEnumerable<MemberInfo> GetSerializableMembers(this Type type)
    {
        return type
            .GetMembers(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(info =>
                info.GetCustomAttributesData()
                    .Any(data =>
                        data.AttributeType == typeof(SerializeField) ||
                        data.AttributeType == typeof(SerializeReference)))
            .Concat(type.GetMembers(BindingFlags.Public | BindingFlags.Instance))
            .Where(m =>
            {
                if (m.MemberType == MemberTypes.Field)
                    return true;

                if (m.MemberType == MemberTypes.Property)
                {
                    PropertyInfo prop = ((PropertyInfo)m);

                    if (!prop.CanWrite || !prop.CanRead)
                        return false;

                    return prop.DeclaringType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                        .Any(f => f.Name.Contains("<" + m.Name + ">"));
                }

                return false;
            });
    }
    
    public static IEnumerable<MemberInfo> GetMembersWithAttribute<TAttribute>(this Type type) where TAttribute : Attribute 
    {
        return type
            .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .MembersWithAttribute<TAttribute>();
    }
    
    public static IEnumerable<MemberInfo> MembersWithAttribute<TAttribute>(this IEnumerable<MemberInfo> type) where TAttribute : Attribute 
    {
        return type
            .Where(info =>
                info.GetCustomAttributesData()
                    .Any(data => data.AttributeType == typeof(TAttribute)));
    }
}
