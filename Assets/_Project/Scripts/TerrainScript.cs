using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainScript : MonoBehaviour
{
    public Vector2 tileAmount;
    public Material mat;
    public ParticleSystem pSys;
    private TerrainData _terraindata;
    // Start is called before the first frame update
    void Start()
    {
        pSys.Stop();
        _terraindata = new TerrainData();
       
        _terraindata.SetDetailResolution(66, 66);
        GameObject terrain = Terrain.CreateTerrainGameObject(_terraindata);
        Terrain T = terrain.transform.GetComponent<Terrain>();
        T.materialTemplate = mat;
        terrain.transform.position = transform.position;

        float[,] heightmaps= new float [_terraindata.heightmapResolution, _terraindata.heightmapResolution];
        for (int i = 0; i < _terraindata.heightmapResolution; ++i)
        {
            
            for (int j = 0; j < _terraindata.heightmapResolution; ++j)
            {
                heightmaps[i, j] = 1f;
            }
        }
        _terraindata.SetHeightsDelayLOD(0,0, heightmaps);

    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public IEnumerator makeHole(int x, int y)
    {
        pSys.transform.position = new Vector3(x - 10, 1, y - 6);
        pSys.Play();
        for (float dist = 1f; dist  > 0.2f; dist -= (0.3f * Time.deltaTime))
        {

            _terraindata.SetHeightsDelayLOD(x, y, new float[,] { { dist } });
            yield return new WaitForEndOfFrame();
        }
        pSys.Stop();

    }



}
