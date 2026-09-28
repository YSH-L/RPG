using System;
using System.Collections.Generic;

/// <summary>
/// 저장 파일에 들어가는 내용. JsonUtility로 그대로 직렬화된다.
/// 에셋은 참조 대신 <b>에셋 이름</b>으로 적는다 (Item_IronSword, BossStats_FireWorm, Area_Field2).
/// </summary>
[Serializable]
public class SaveData
{
    public int version = 1;

    // 캐릭터
    public int level = 1;
    public int exp;
    public int gold;
    public List<ItemStack> items = new List<ItemStack>();
    public string weapon;
    public string armor;

    // 진행
    public int questIndex;
    public bool questAccepted;
    public int[] questKills = new int[0];
    public List<string> defeatedBosses = new List<string>();
    public string area;
}

[Serializable]
public class ItemStack
{
    public string item;
    public int count;
}
