//-----------------------------------------------------------------------------
// Copyright(c) 2016 Jean Simonet - http://jeansimonet.com
// Distributed under the MIT License - https://opensource.org/licenses/MIT
//-----------------------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using System.Linq;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Coroutines
{
	//public static Dictionary<int, Object> StoredObjects = new Dictionary<int, Object>();

	//public class UnityObjectConverter : JsonConverter
	//{
	//	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	//	{
	//		var obj = value as UnityEngine.Object;
	//		int objID = obj.GetInstanceID();
	//		StoredObjects[objID] = obj;

	//		writer.WriteStartObject();
	//		writer.WritePropertyName("ObjectID");
	//		writer.WriteValue(objID);
	//		writer.WriteEndObject();
	//	}

	//	public override object ReadJson(JsonReader reader, System.Type objectType, object existingValue, JsonSerializer serializer)
	//	{
	//		JObject obj = JObject.Load(reader);
	//		int id = obj["ObjectID"].Value<int>();
	//		UnityEngine.Object ret = null;
	//		StoredObjects.TryGetValue(id, out ret);
	//		return ret;
	//	}

	//	public override bool CanRead
	//	{
	//		get
	//		{
	//			return true;
	//		}
	//	}

	//	public override bool CanConvert(System.Type objectType)
	//	{
	//		return typeof(UnityEngine.Object).IsAssignableFrom(objectType);
	//	}
	//}



	//public class DelegateConverter : JsonConverter
	//{
	//	[System.Serializable]
	//	public class DelegateInfo
	//	{
	//		// MethodInfo
	//		public string DelegateTypeName;
	//		public string DeclaringTypeName;
	//		public string MethodName;
	//		public string[] MethodParameterTypes;

	//		// Target
	//		public object Target;
	//	}

	//	public DelegateInfo ToDelegateInfo(System.Delegate funcDelegate)
	//	{
	//		DelegateInfo ret = new DelegateInfo();

	//		// Get the delegate's actual type
	//		System.Type delegateType = funcDelegate.GetType();

	//		// Save the MethodInfo
	//		ret.DelegateTypeName = delegateType.AssemblyQualifiedName;
	//		ret.DeclaringTypeName = funcDelegate.Method.DeclaringType.AssemblyQualifiedName;
	//		ret.MethodName = funcDelegate.Method.Name;
	//		var paramInfo = funcDelegate.Method.GetParameters();
	//		ret.MethodParameterTypes = new string[paramInfo.Length];
	//		for (int i = 0; i < paramInfo.Length; ++i)
	//		{
	//			ret.MethodParameterTypes[i] = paramInfo[i].ParameterType.AssemblyQualifiedName;
	//		}

	//		// Save the Target
	//		ret.Target = funcDelegate.Target;

	//		return ret;
	//	}

	//	public System.Delegate FromDelegateInfo(DelegateInfo info)
	//	{
	//		System.Type delegateType = System.Type.GetType(info.DelegateTypeName);
	//		System.Type declaringType = System.Type.GetType(info.DeclaringTypeName);
	//		System.Type[] parameterTypes = new System.Type[info.MethodParameterTypes.Length];
	//		for (int i = 0; i < info.MethodParameterTypes.Length; ++i)
	//		{
	//			string paramType = info.MethodParameterTypes[i];
	//			parameterTypes[i] = System.Type.GetType(paramType);
	//		}

	//		MethodInfo method = declaringType.GetMethod(info.MethodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, parameterTypes, null);

	//		return System.Delegate.CreateDelegate(delegateType, info.Target, method);
	//	}

	//	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	//	{
	//		var del = value as System.Delegate;
	//		var info = ToDelegateInfo(del);
	//		serializer.Serialize(writer, info);
	//	}

	//	public override object ReadJson(JsonReader reader, System.Type objectType, object existingValue, JsonSerializer serializer)
	//	{
	//		DelegateInfo info = new DelegateInfo();
	//		serializer.Populate(reader, info);
	//		return FromDelegateInfo(info);
	//	}

	//	public override bool CanRead
	//	{
	//		get
	//		{
	//			return true;
	//		}
	//	}

	//	public override bool CanConvert(System.Type objectType)
	//	{
	//		return typeof(System.Delegate).IsAssignableFrom(objectType);
	//	}
	//}

	//public void TestSaveToJson()
	//{
	//	var saved = ToJson();
	//	_Func = null;
	//	//FromJson(saved);
	//}

	//public class CustomContractResolver
	//	: DefaultContractResolver
	//{
	//	protected override List<MemberInfo> GetSerializableMembers(System.Type objectType)
	//	{
	//		if (typeof(UnityEngine.MonoBehaviour).IsAssignableFrom(objectType))
	//		{
	//			var fields = objectType.GetMembers(BindingFlags.NonPublic | BindingFlags.Instance).Where(f => f.MemberType == MemberTypes.Field);
	//			return new List<MemberInfo>(fields);
	//		}
	//		else
	//		{
	//			return base.GetSerializableMembers(objectType);
	//		}
	//	}
	//}

	//public string ToJson()
	//{
	//	var settings = new JsonSerializerSettings();
	//	settings.Converters.Add(new DelegateConverter());
	//	//settings.Converters.Add(new UnityObjectConverter());
	//	settings.Formatting = Formatting.Indented;
	//	settings.PreserveReferencesHandling = PreserveReferencesHandling.All;

	//	settings.ContractResolver = new CustomContractResolver();

	//	string ret = JsonConvert.SerializeObject(_Func, settings);

	//	System.IO.File.WriteAllText("output.json", ret);

	//	Debug.Log(ret);
	//	return ret;
	//}

	//public void FromJson(string jsonString)
	//{
	//	var settings = new JsonSerializerSettings();
	//	settings.Converters.Add(new DelegateConverter());
	//	//settings.Converters.Add(new UnityObjectConverter());
	//	var traceWriter = new MemoryTraceWriter();
	//	traceWriter.LevelFilter = System.Diagnostics.TraceLevel.Verbose;
	//	settings.TraceWriter = traceWriter;

	//	_Func = JsonConvert.DeserializeObject<System.Func<float, int, bool>>(jsonString, settings);
	//	Debug.Log(traceWriter);
	//}

}