using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using TMPro;
using UnityEngine;

namespace SnowDay.Snowfort
{
    public class Cannon : MonoBehaviour, Controllable, BuildingOverride
    {
        public float swingSpeed;

        float angle;

        public float swingAngleUpper;
        public float swingAngleLower;

        public float launchVelocity;

        public float cooldown = 1f;

        public GameObject ammo;

        public BuildingObject restockObject;
        public int restockCount;
        public GameObject restockPreview;

        public TextMeshPro clipCounter;

        int ammoCount = 6;
        bool active;

        bool rightTeam;

        float cooldownTimer;

        Transform launcher;
        Transform launchPoint;

        int leftTeamMask;
        int rightTeamMask;

        public bool CanUse()
        {
            return ammoCount > 0;
        }

        public void Use()
        {
            if (ammoCount <= 0 || cooldownTimer > 0) return;

            ammoCount--;
            GameObject shot = Instantiate(ammo, launchPoint.position, launchPoint.rotation);
            float rad = (launchPoint.eulerAngles.z + 90) * 2 * Mathf.PI / 360;
            shot.GetComponent<Rigidbody2D>().linearVelocity = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * launchVelocity;
            
            TeamIgnore t = shot.GetComponent<TeamIgnore>();
            if (t != null)
            {
                shot.layer = rightTeam ? rightTeamMask : leftTeamMask;
            }

            clipCounter.text = ammoCount.ToString();

            cooldownTimer = cooldown;
        }

        public void SetActive(bool a)
        {
            active = a;
        }

        public bool OverrideExists(BuildingObject obj)
        {
            if (obj == restockObject)
                return true;

            return false;
        }

        public bool CanOverride(BuildingObject obj)
        {

            if (obj == restockObject)
                return true;

            return false;
        }

        public BuildingOverrideData OverrideData(BuildingObject obj)
        {
            if (obj == restockObject)
                return new BuildingOverrideData(restockPreview, true);

            return new BuildingOverrideData();
        }

        public void BuildOverride(BuildingObject obj)
        {
            if (obj == restockObject)
            {
                ammoCount += restockCount;
                clipCounter.text = ammoCount.ToString();
            }
        }

        void Update()
        {
            cooldownTimer -= Time.deltaTime;

            if (!active) return;

            angle += swingSpeed * Mathf.PI * Time.deltaTime;
            launcher.localEulerAngles = new Vector3(0, 0, swingAngleLower + (swingAngleUpper - swingAngleLower) * 
                (Mathf.Sin(angle)/2 + 0.5f));
        }

        public void SetTeam(bool rTeam)
        {
            rightTeam = rTeam;
        }

        void Start()
        {
            launcher = transform.GetChild(0);
            launchPoint = transform.GetChild(0).GetChild(0);

            GameManager g = FindObjectOfType<GameManager>();
            leftTeamMask = g.leftTeam;
            rightTeamMask = g.rightTeam;

            clipCounter.text = ammoCount.ToString();
        }
    }
}