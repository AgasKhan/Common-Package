using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public enum Dependency
{

	///<summary> Type: System.Int32 <br/>Actual value: 106 </summary>
	PlayerLife5, 
	///<summary> Type: System.String <br/>Actual value: casiiita </summary>
	PlayerName, 
	///<summary> Type: UnityEngine.MonoBehaviour <br/>Actual value: SphereGPU (GPUInstancing.GPUInstancingComponent) </summary>
	Mono, 
	///<summary> Type: UnityEngine.Vector2 <br/>Actual value: (0.00, 0.00) </summary>
	asf=0,

	///<summary> Type: System.Int32 <br/>Actual value: 63 </summary>
	PlayerLife3=1,

}

public partial class DefaultDependencyAttribute
{
	public DefaultDependencyAttribute(Dependency dependency)
	{
		index = (int)dependency;
	}

	public DefaultDependencyAttribute(string name)
	{
		index = (int)System.Enum.Parse<Dependency>(name);
	}
}