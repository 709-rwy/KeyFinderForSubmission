using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//迷路生成の際に基本単位となる四角い部屋をプログラムで扱うためのクラス
//エディタ上でのみ使用するため、UdonSharpBehaviourではなくMonoBehaviourを継承する。
#if UNITY_EDITOR
public class room : MonoBehaviour
{
    //下位ビットから+Z、+X、-Z、-Xの順に、各方向の壁に通路があるかを表す4ビット値。
    [SerializeField]private int doorBit;

    public const int WALL_NUM = 4;
    public const int BIT_MASK = 0b1111;

    //必要な通路配置と一致するまで90度単位で回転させ、その回転回数を返す。一致する配置がない場合は-1を返す。
    public int GetMatchRotation(int otherBit)
    {
        for (int i = 0;i < WALL_NUM; i++)
        {
            if(doorBit == otherBit) return i;
            otherBit = ((otherBit >> 1) | (otherBit << (WALL_NUM - 1))) & BIT_MASK;
        }
        return -1;
    }

    public void setDoorBit(int bit) => doorBit = bit;
}
#endif