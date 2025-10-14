using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ServerDeckSO", menuName = "ScriptableObjects/ServerDeckSO", order = 2)]
public class ServerDeckSO : ScriptableObject
{
    public List<CardData> cards;
}