
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


//Udonの場合、クラスそのものを型として扱えない(クラスを引数として扱えなかったりジェネリクス型が扱えない)ので
//それらを扱うため列挙型を使ってごまかしている
public enum ItemType
{
    handCuff,
    key,
    flashLight
}
