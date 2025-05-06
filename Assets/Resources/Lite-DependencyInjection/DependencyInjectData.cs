using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
public partial class DependencyInjectData
{
	public IEnumerable<KeyValuePair> All => INT32.Concat(STRING).Concat(VECTOR3).Concat(MONOBEHAVIOUR).Concat(VECTOR2);


	
	[SerializeField]
	public KeyValuePairArray<System.Int32> INT32;
	
	[SerializeField]
	public KeyValuePairArray<System.String> STRING;
	
	[SerializeField]
	public KeyValuePairArray<UnityEngine.Vector3> VECTOR3;
	
	[SerializeField]
	public KeyValuePairArray<UnityEngine.MonoBehaviour> MONOBEHAVIOUR;
	
	[SerializeField]
	public KeyValuePairArray<UnityEngine.Vector2> VECTOR2;
}

