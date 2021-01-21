using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;

public class ParticleWin : MonoBehaviour
{
    [SerializeField] public List<GameObject> particle;
    public int numberParticle;
    private UnityEvent m_particlePlay;

    void Start()
    {
        if (m_particlePlay == null)
            m_particlePlay = new UnityEvent();
        
        m_particlePlay.AddListener(ParticlePlay);
        numberParticle = 0;
    }

    public void ParticlePlay()
    {
        if (numberParticle < 1)
            particle[numberParticle].GetComponent<ParticleSystem>().Play();
        else
        {
            particle[numberParticle].GetComponent<VisualEffect>().enabled = true;
            particle[numberParticle].GetComponent<VisualEffect>().Play();
        }
    }
    
}
