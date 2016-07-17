using UnityEngine;
using System.Collections;

public class PortalScript : MonoBehaviour {
    public bool paused;
    // track the position of each portal
    public enum WallOrientation {
        Ceiling,
        Right,
        Floor,
        Left
    }

    public struct PortalPosition {
        public Vector2 p1;
        public Vector2 p2;
        public WallOrientation p1Or;
        public WallOrientation p2Or;

        public PortalPosition(Vector2 p1, Vector2 p2) {
            this.p1 = p1;
            this.p2 = p2;
            this.p1Or = WallOrientation.Left;
            this.p2Or = WallOrientation.Left;
        }
    }

    //public GameObject portalPrefab;
    public GameObject P1Prefab, P2Prefab;
    public GameObject Portal1, Portal2;
    public PortalPosition PPos;
    private WallOrientation shotOr;
    private float portalLength;
    public bool p1underwater;
    public bool p2underwater;

    private GameObject player;

    // Use this for initialization
    void Awake() {
        Portal1 = GameObject.Instantiate(P1Prefab);
        Portal2 = GameObject.Instantiate(P2Prefab);
        Portal1.SetActive(false);
        Portal2.SetActive(false);
        portalLength = Portal1.GetComponent<BoxCollider2D>().size.y*3;
        PPos = new PortalPosition(Portal1.transform.position, Portal2.transform.position);
        player = GameObject.Find("Player");
        p1underwater = false;
        p2underwater = false;
    }

    // Update is called once per frame
    void Update() {
        //Create ray cast from player position to the platform
        if ((Input.GetButtonDown("Fire1") || Input.GetButtonDown("Fire2")) && !paused
                && player.GetComponent<playerController>().GetPlayerState() == playerController.State.Portals) {
            int platforms = LayerMask.GetMask("PortalPlatform", "Platform", "Barrier");
            int badportal;
            if (Input.GetButtonDown("Fire1")) {
                badportal = LayerMask.GetMask("Portal2");
            }
            else {
                badportal = LayerMask.GetMask("Portal1");
            }
            int pplatform = LayerMask.NameToLayer("PortalPlatform");

            Vector2 pos = transform.position;

            Vector2 target = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 direction = target - pos;

            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, Mathf.Infinity, platforms | badportal);

            if (hit && hit.collider.gameObject.layer == pplatform) {
                Quaternion rotation = GetPortalRotation(hit);

                bool makePortal = true;
                Vector2 consol = PortalPositionConsolidation(ref makePortal, hit);
                Vector3 portalPos = new Vector3(consol.x, consol.y, 0);

                if (makePortal) {
                    if (Input.GetButtonDown("Fire1"))
                        p1underwater = CheckUnderwater(portalPos, hit.normal);
                    else
                        p2underwater = CheckUnderwater(portalPos, hit.normal);
                }

                if (Input.GetButtonDown("Fire1") && makePortal) //If right mouse click
                {
                    Portal1.SetActive(true);
                    Portal1.transform.rotation = rotation;
                    Portal1.transform.position = portalPos - (Portal1.transform.right * .2f);
                        
                    PPos.p1 = Portal1.transform.position;
                    PPos.p1Or = shotOr;
                } else if (Input.GetButtonDown("Fire2") && makePortal) //if left mouse click
                  {
                    Portal2.SetActive(true);
                    Portal2.transform.rotation = rotation;
                    Portal2.transform.position = portalPos - (Portal2.transform.right * .2f);
                    PPos.p2 = Portal2.transform.position;
                    PPos.p2Or = shotOr;
                }
            }
        }
    }

    //Find the correct orientation of the portal
    private Quaternion GetPortalRotation(RaycastHit2D hit) {
        Quaternion rot;

        if (hit.normal.y == 1) {
            shotOr = WallOrientation.Floor;
            rot = Quaternion.Euler(0, 0, 270);
        } else if (hit.normal.y == -1) {
            shotOr = WallOrientation.Ceiling;
            rot = Quaternion.Euler(0, 0, 90);
        } else if (hit.normal.x == 1) {
            shotOr = WallOrientation.Left;
            rot = Quaternion.Euler(0, 0, 180);
        } else {
            shotOr = WallOrientation.Right;
            rot = Quaternion.identity;
        }
        return rot;
    }

    private Vector2 PortalPositionConsolidation(ref bool makePortal, RaycastHit2D hit) {
        Vector2 origin = hit.point;
        bool vertical = (shotOr == WallOrientation.Left || shotOr == WallOrientation.Right);
        Vector2 normal = hit.normal;
        int whichPortal;
        if (Input.GetButtonDown("Fire1"))
            whichPortal = 1;
        else
            whichPortal = 2;

        int tries = 0;
        Vector2 result = origin;
        float maxChange = 0.0f;

        while (tries < 2) {
            Vector2 cornerPos = DetectCorner(result, normal, vertical, whichPortal);
            Vector2 overHangPos = DetectOverhang(result, normal, vertical, whichPortal);
            Vector2 gapPos = DetectPlatformGap(result, normal, vertical, whichPortal);

            float cornerChange = (result - cornerPos).sqrMagnitude;
            float overHangChange = (result - overHangPos).sqrMagnitude;
            float gapChange = (result - gapPos).sqrMagnitude;

            maxChange = Mathf.Max(cornerChange, overHangChange, gapChange);
            if (maxChange < 0.01f) //Basically no change
                return result;
            else if (overHangChange == maxChange)
                result = overHangPos;
            else if (gapChange == maxChange)
                result = gapPos;
            else if (cornerChange == maxChange)
                result = cornerPos;
            tries++;
        }

        //Tried twice and failed
        makePortal = false;
        return result;
    }

    private Vector2 DetectCorner(Vector2 origin, Vector2 normal, bool vertical, int whichPortal) {
        Vector2 newPos = origin;
        float offsetConst = .1f; // this moves the raycast away from the wall to not detect the wall that was hit
        float move;
        int platforms = LayerMask.GetMask("Platform", "PortalPlatform");
        int badportal;
        if (whichPortal == 1)
            badportal = LayerMask.GetMask("Portal2");
        else
            badportal = LayerMask.GetMask("Portal1");

        if (vertical) {
            float dir = Mathf.Sign(normal.x);
            Vector2 rayOrigin = new Vector2(origin.x + offsetConst * dir, origin.y);
            RaycastHit2D downRay = Physics2D.Raycast(rayOrigin, Vector2.down, portalLength, platforms | badportal);
            RaycastHit2D upRay = Physics2D.Raycast(rayOrigin, Vector2.up, portalLength, platforms | badportal);

            Debug.DrawRay(rayOrigin, Vector2.up, Color.white, 0.2f);
            Debug.DrawRay(rayOrigin, Vector2.down, Color.white, 0.2f);

            if (downRay) {
                Debug.DrawRay(rayOrigin, Vector2.down, Color.red, 0.2f);
                move = portalLength + downRay.point.y;
                newPos.y = move;
            } else if (upRay) {
                Debug.DrawRay(rayOrigin, Vector2.up, Color.red, 0.2f);
                move = upRay.point.y - portalLength;
                newPos.y = move;
            }
        } else {
            float dir = Mathf.Sign(normal.y);
            Vector2 rayOrigin = new Vector2(origin.x, offsetConst * dir + origin.y);
            RaycastHit2D leftRay = Physics2D.Raycast(rayOrigin, Vector2.left, portalLength, platforms | badportal);
            RaycastHit2D rightRay = Physics2D.Raycast(rayOrigin, Vector2.right, portalLength, platforms | badportal);

            Debug.DrawRay(rayOrigin, Vector2.left, Color.white, 0.2f);
            Debug.DrawRay(rayOrigin, Vector2.right, Color.white, 0.2f);

            if (leftRay) {
                Debug.DrawRay(rayOrigin, Vector2.left, Color.red, 0.2f);
                move = portalLength + leftRay.point.x;
                newPos.x = move;
            } else if (rightRay) {
                Debug.DrawRay(rayOrigin, Vector2.right, Color.red, 0.2f);
                move = rightRay.point.x - portalLength;
                newPos.x = move;
            }
        }
        return newPos;
    }

    private Vector2 DetectOverhang(Vector2 origin, Vector2 normal, bool vertical, int whichPortal) {
        Vector2 newPos = origin;

        RaycastHit2D findPlatform;
        int portalPlatform = LayerMask.GetMask("PortalPlatform");

        //Get Portal Edge coordinates
        Vector2 leftEdge = origin;
        Vector2 rightEdge = origin;
        float inset = 0.001f;
        float newLength = portalLength - inset;
        if (vertical) {
            rightEdge.y += newLength;
            leftEdge.y -= newLength;
        } else {
            rightEdge.x += newLength;
            leftEdge.x -= newLength;
        }

        //Shoot raycasts
        float raycastDist = 0.01f;

        RaycastHit2D hit0 = Physics2D.Raycast(rightEdge, -normal, raycastDist, portalPlatform);
        RaycastHit2D hit1 = Physics2D.Raycast(leftEdge, -normal, raycastDist, portalPlatform);

        //Testing
        Debug.DrawRay(leftEdge, -normal, Color.yellow, .2f);
        Debug.DrawRay(rightEdge, -normal, Color.yellow, .2f);

        float move;

        if (!hit0 && vertical) { //right edge hanging off (too high)
            Debug.DrawRay(rightEdge, -normal, Color.blue, .2f);
            // find the new raycast origin
            rightEdge.x = rightEdge.x + raycastDist * -normal.x;
            findPlatform = Physics2D.Raycast(rightEdge, Vector2.down, portalLength, portalPlatform);
            move = rightEdge.y - findPlatform.point.y + inset;
            newPos.y -= move;
        } else if (!hit1 && vertical) { //left edge hanging off (too low)
            Debug.DrawRay(leftEdge, -normal, Color.blue, .2f);
            //find new origin below
            leftEdge.x = leftEdge.x + raycastDist * -normal.x;
            findPlatform = Physics2D.Raycast(leftEdge, Vector2.up, portalLength, portalPlatform);
            move = findPlatform.point.y - leftEdge.y - inset;
            newPos.y += move;
        } else if (!hit0 && !vertical) {
            Debug.DrawRay(rightEdge, -normal, Color.blue, .2f);
            rightEdge.y = rightEdge.y + raycastDist * -normal.y;
            findPlatform = Physics2D.Raycast(rightEdge, Vector2.left, portalLength, portalPlatform);
            move = rightEdge.x - findPlatform.point.x + inset;
            newPos.x -= move;
        } else if (!hit1 && !vertical) {
            Debug.DrawRay(leftEdge, -normal, Color.blue, .2f);
            //find new origin below
            leftEdge.y = leftEdge.y + raycastDist * -normal.y;
            findPlatform = Physics2D.Raycast(leftEdge, Vector2.right, portalLength, portalPlatform);
            move = findPlatform.point.x - leftEdge.x - inset;
            newPos.x += move;
        }
        return newPos;
    }

    private Vector2 DetectPlatformGap(Vector2 origin, Vector2 normal, bool vertical, int whichPortal) {
        Vector2 newPos = origin;

        //offset into the wall
        float offset = 0.01f;

        //Platform only layer mask
        int platformOnly = LayerMask.GetMask("Platform");

        //set raycast origin position to be in the wall
        if (vertical) {
            origin.x = origin.x - offset * normal.x;
        } else {
            origin.y = origin.y - offset * normal.y;
        }

        //Shoot raycasts
        float gapMove = 0.0f;

        if (vertical) {
            RaycastHit2D hit0 = Physics2D.Raycast(origin, Vector2.down, portalLength, platformOnly);
            RaycastHit2D hit1 = Physics2D.Raycast(origin, Vector2.up, portalLength, platformOnly);

            if (hit0) {
                Debug.DrawRay(origin, Vector2.down, Color.red, 0.2f);
                gapMove = hit0.point.y + portalLength;
                newPos.y = gapMove;
            } else if (hit1) {
                Debug.DrawRay(origin, Vector2.up, Color.red, 0.2f);
                gapMove = hit1.point.y - portalLength;
                newPos.y = gapMove;
            }
        } else { //Horizontal portal
            RaycastHit2D hit0 = Physics2D.Raycast(origin, Vector2.left, portalLength, platformOnly);
            RaycastHit2D hit1 = Physics2D.Raycast(origin, Vector2.right, portalLength, platformOnly);

            if (hit0) {
                Debug.DrawRay(origin, Vector2.left, Color.red, 0.2f);
                gapMove = hit0.point.x + (portalLength + 0.01f);
                newPos.x = gapMove;
            } else if (hit1) {
                Debug.DrawRay(origin, Vector2.right, Color.red, 0.2f);
                gapMove = hit1.point.x - (portalLength + 0.01f);
                newPos.x = gapMove;
            }
        }

        return newPos;
    }

    private bool CheckUnderwater(Vector2 origin, Vector2 hitNormal) {
        float offsetConst = 0.1f;
        float dir = 1f;
        LayerMask waterMask = LayerMask.GetMask("Water");

        if (hitNormal.y == 0) {
            if (hitNormal.x < 0)
                dir = -1f;

            Vector2 rayOrigin = new Vector2(origin.x + offsetConst * dir, origin.y);
            RaycastHit2D upRay = Physics2D.Raycast(rayOrigin, Vector2.up, portalLength, waterMask);

            if (upRay) 
                return true;
            return false;
        } else {
            if (hitNormal.y < 0)
                dir = -1f;

            Vector2 rayOrigin = new Vector2(origin.x, origin.y + offsetConst * dir);
            RaycastHit2D leftRay = Physics2D.Raycast(rayOrigin, Vector2.left, portalLength, waterMask);
            RaycastHit2D rightRay = Physics2D.Raycast(rayOrigin, Vector2.right, portalLength, waterMask);

            if (leftRay || rightRay)
                return true;
            return false;
        }
    }
}

