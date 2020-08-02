using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Planetoid : MonoBehaviour
{
    public PlanetoidChunk PlanetoidChunkPrefab;

    public PlanetoidSizeSetting SizeSetting;
    public SimpleNoiseSetting[] NoiseSettings;

    private PlanetoidMapEngine Engine;
    private NoiseGenerator GenNoise;
    private PlanetoidPlayer Player;
    private PlanetoidMapEngineOperation Operations;

    public Planetoid()
    {
        Operations = new PlanetoidMapEngineOperation(UpdateTree,CreateTree,DeleteTree
            ,WorldSpacePlayerLocation,WorldSpacePlanetLocation
            ,IsTreeCreated);
    }

    void Start()
    {
        name = SizeSetting.PlanetName;
 
        Player = FindObjectOfType<PlanetoidPlayer>();
        Engine = new PlanetoidMapEngine(SizeSetting, Operations);
        
        GenNoise = new NoiseGenerator(NoiseSettings);

        Engine.Init();

        /*List<PlanetoidQuadTree> Leafs = Engine.Leafs;
        Leafs.ForEach((PlanetoidQuadTree Tree) =>
        {

        });*/
    }

    
    void Update()
    {
        
    }

    public void UpdateTree(PlanetoidQuadTree Tree)
    {
        
    }

    public void CreateTree(PlanetoidQuadTree Tree)
    {
        PlanetoidChunk NewChunk = Instantiate(PlanetoidChunkPrefab, transform.position, Quaternion.identity);
        NewChunk.transform.parent = transform;
        NewChunk.name = Tree.Id;

        Noise2DData NoiseData = GenNoise.Noise2D(SizeSetting, Tree.Start, Tree.Size, 1, Tree.Face);

        MeshGenerator MeshGen = new MeshGenerator(SizeSetting,NoiseData,Tree);
        NewChunk.Instantiate(this, MeshGen);
    }

    public void DeleteTree(PlanetoidQuadTree Tree)
    {
        Destroy(transform.Find(Tree.Id).gameObject);
    }

    public bool IsTreeCreated(PlanetoidQuadTree Tree)
    {
        return transform.Find(Tree.Id) != null;
    }

    public Vector3 WorldSpacePlayerLocation()
    {
        return Player.transform.position;
    }

    public Vector3 WorldSpacePlanetLocation()
    {
        return transform.position;
    }

}
