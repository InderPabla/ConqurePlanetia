using System.Collections;
using System.Collections.Generic;
using UnityEngine;




public class Store<T>
{
    public string StoreId;
    public T StorageItem;
    public long TimeCreatedMs;
    public long TimeAliveMs;
    public long TimeExpiredMs;
    public System.Diagnostics.Stopwatch Watch;

    public Store(System.Diagnostics.Stopwatch watch, string storeId, T storageItem, long timeAliveMs) {
        Watch = watch;
        StoreId = storeId;
        StorageItem = storageItem;
        TimeCreatedMs = watch.ElapsedMilliseconds;
        TimeAliveMs = timeAliveMs;
        TimeExpiredMs = TimeCreatedMs + TimeAliveMs;
    }

    public bool IsExpired
    {
        get
        {
            return Watch.ElapsedMilliseconds >= TimeExpiredMs;
        } 
    }
}

public class GrassStore : List<List<Matrix4x4>> {}



/*public class GrassStore : Store
{
    public List<List<Matrix4x4>> GrassData;
    public long CreatedTimestampMs;

    public GrassStorage(List<List<Matrix4x4>> grassData, long createdTimestampMs)
    {
        GrassData = grassData;
        CreatedTimestampMs = createdTimestampMs;
    }
}

public class ChunkStore
{
    public ChunkData MeshData;
    public long CreatedTimestampMs;

    public ChunkStorage(ChunkData meshData, long createdTimestampMs)
    {
        MeshData = meshData;
        CreatedTimestampMs = createdTimestampMs;
    }
}*/
