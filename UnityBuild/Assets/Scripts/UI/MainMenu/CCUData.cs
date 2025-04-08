using UnityEngine;
using System;
using System.Collections.Generic;


[Serializable]
public class UserInfo
{
    public string nickName;
    public string status;
}

[Serializable]
public class CCUMessage
{
    public string action;
    public List<UserInfo> users;
}