using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class WorldManager : MonoBehaviour
{
    private List<Planetoid> Planets;

    void Start()
    {
        Planets = FindObjectsOfType<Planetoid>().ToList<Planetoid>();
    }
    
    void Update()
    {
        
    }

    void OnApplicationQuit()
    {
        Debug.Log("Deleteing MeshGeneratorDataStorage, and killing thread");
        ChunkDataStorage.GetInstance().StorageCleanUp();
        ChunkDataStorage.GetInstance().Kill();
    }
}
