//#define DEBUG_AsteraX_LogMethods

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteraX : MonoBehaviour
{
    // Private Singleton-style instance. Accessed by static property S later in script
    static private AsteraX _S;

    static List<Asteroid>           ASTEROIDS;
    static List<Bullet>             BULLETS;

    const float MIN_ASTEROID_DIST_FROM_PLAYER_SHIP = 5;

    //static public CallbackDelegate GAME_STATE_CHANGE_DELEGATE;
    public delegate void CallbackDelegateV3(Vector3 v);

    [System.flags]
    public enum eGameState
    {
      none = 0,
      mainMenu=1,
      preLevel=2,
      level=4,
      postLevel=8,
      gameOver=16,
      all = 0xFFFFFFF
    }
    [Header("Set in Inspector")]
    [Tooltip("This sets the AsteroidsScriptableObject to be used throughout the game.")]
    public AsteroidsScriptableObject asteroidsSO;


    private void Awake()
    {
#if DEBUG_AsteraX_LogMethods
        Debug.Log("AsteraX:Awake()");
#endif
        S = this;
    }


    void Start()
    {
#if DEBUG_AsteraX_LogMethods
        Debug.Log("AsteraX:Start()");
#endif

        ASTEROIDS = new List<Asteroid>();

        // Spawn the parent Asteroids, child Asteroids are taken care of by them
        for (int i = 0; i < 3; i++)
        {
            SpawnParentAsteroid(i);
        }
    }


    void SpawnParentAsteroid(int i)
    {
#if DEBUG_AsteraX_LogMethods
        Debug.Log("AsteraX:SpawnParentAsteroid("+i+")");
#endif
        Asteroid ast = Asteroid.SpawnAsteroid();
        ast.gameObject.name = "Asteroid_" + i.ToString("00");
        // Find a good location for the Asteroid to spawn
        Vector3 pos;
        do
        {
            pos = ScreenBounds.RANDOM_ON_SCREEN_LOC;
        } while ((pos - PlayerShip.POSITION).magnitude < MIN_ASTEROID_DIST_FROM_PLAYER_SHIP);

        ast.transform.position = pos;
        ast.size = asteroidsSO.initialSize;
    }

    public void EndGame()
    {
      GAME_STATE = eGameState.gameOver();
      Invoke("ReloadScene", DELAY_BEFORE_RELOADING_SCENE);
    }

    void ReloadScene()
    {
      UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
    // ---------------- Static Section ---------------- //

    /// <summary>
    /// <para>This static public property provides some protection for the Singleton _S.</para>
    /// <para>get {} does return null, but throws an error first.</para>
    /// <para>set {} allows overwrite of _S by a 2nd instance, but throws an error first.</para>
    /// <para>Another advantage of using a property here is that it allows you to place
    /// a breakpoint in the set clause and then look at the call stack if you fear that
    /// something random is setting your _S value.</para>
    /// </summary>
    static private AsteraX S
    {
        get
        {
            if (_S == null)
            {
                Debug.LogError("AsteraX:S getter - Attempt to get value of S before it has been set.");
                return null;
            }
            return _S;
        }
        set
        {
            if (_S != null)
            {
                Debug.LogError("AsteraX:S setter - Attempt to set S when it has already been set.");
            }
            _S = value;
        }
    }

    const int RESPAWN_DIVISONS = 8;
    const int RESPAWN_AVOID_EDGES = 2;
    static Vector3[,] RESPAWN_POINTS;

    static public AsteroidsScriptableObject AsteroidsSO
    {
        get
        {
            if (S != null)
            {
                return S.asteroidsSO;
            }
            return null;
        }
    }

	static public void AddAsteroid(Asteroid asteroid)
    {
        if (ASTEROIDS.IndexOf(asteroid) == -1)
        {
            ASTEROIDS.Add(asteroid);
        }
    }
    static public void RemoveAsteroid(Asteroid asteroid)
    {
        if (ASTEROIDS.IndexOf(asteroid) != -1)
        {
            ASTEROIDS.Remove(asteroid);
        }
    }

    static public void GameOver()
    {
      _S.EndGame();
    }
    static public IEnumerator FindRespawnPointCoroutine(Vector3 prevPos, CallbackDelegateV3  callback)
    {
      if(RESPAWN_POINTS==null)
      {
        RESPAWN_POINTS = new Vector3[RESPAWN_DIVISONS + 1, RESPAWN_DIVISONS +1];
        Bounds playAreaBounds = ScreenBounds.BOUNDS;
        float dx = playAreaBounds.size.x / RESPAWN_DIVISONS;
        float dy = playAreaBounds.size.y / RESPAWN_DIVISONS;
        for(int i=0; i <=RESPAWN_DIVISONS; i++)
        {
          for (int j=0; j <=RESPAWN_DIVISONS; j++)
          {
            RESPAWN_POINTS[i, j] = new Vector3(
            playAreaBounds.min.x + i * dx, playAreaBounds.min.y * dy, 0);
          }
        }
      }
      yield return new WaitForSeconds(PlayerShip.RESPAWN_DELAY * 0.8f);
      float distSqr, closestDistSqr = float.MaxValue;
      int prevI = 0, prevJ=0;

      for(int i = RESPAWN_AVOID_EDGES; i<=RESPAWN_DIVISONS - RESPAWN_AVOID_EDGES; i++)
      {
        for(int j=RESPAWN_AVOID_EDGES; j<=RESPAWN_DIVISONS - RESPAWN_AVOID_EDGES; j++)
        {
          distSqr = (RESPAWN_POINTS[i, j] - prevPos).sqrMagnitude;
          if(distSqr < closestDistSqr)
          {
            closestDistSqr = distSqr;
            prevI = i;
            prevJ = j;
          }
        }
      }
      float furthestDistSqr = 0;
      Vector3 nextPos = prevPos;
      for(int i = RESPAWN_AVOID_EDGES; i<=RESPAWN_DIVISONS -RESPAWN_AVOID_EDGES; i++)
      {
        for(int j = RESPAWN_AVOID_EDGES; i<=RESPAWN_DIVISONS-RESPAWN_AVOID_EDGES; j++)
        {
          if (i==prevI && j==prevJ)
          {
            continue;
          }
          closestDistSqr = float.MaxValue;
          for(int k=0; k<ASTEROIDS.Count; k++)
          {
            distSqr=(ASTEROIDS[k].trasnform.position - RESPAWN_POINTS[i, j]).sqrMagnitude;
            if(distSqr<closestDistSqr)
            {
              closestDistSqr=distSqr;
            }
          }
          if(closestDistSqr > furthestDistSqr)
          {
            furthestDistSqr=closestDistSqr;
            nextPos = RESPAWN_POINTS[i, j];
          }
        }
      }
      //Instantiate(S.respawnAppearParticlesPrefab, nextPos, Quaternion.identity);
      yield return new WaitForSeconds(PlayerShip.RESPAWN_DELAY * 0.2f);
      callback(nextPos);
    }

}
