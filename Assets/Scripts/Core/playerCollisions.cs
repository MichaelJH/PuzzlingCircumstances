using UnityEngine;
using System.Collections;

/* Based on Nicholas DiMucci's platformer solution, modified for objects with 2D components
    overdevelop.blogspot.com, 2013
*/

public class playerCollisions : MonoBehaviour {
    private BoxCollider2D _collider;
    private Rect _collisionRect;
    private LayerMask _collisionMask;
    private LayerMask _portalMask;
    private LayerMask _buttonMask;

    private float xSkinSpace = 0.005f;
    private float ySkinSpace = 0.0005f;

    public bool onGround { get; set; }
    public bool touchedPortal { get; set; }
    public bool pushedButton { get; set; }
    public bool exitedButton { get; set; }
    public bool enteredButton { get; set; }
    public bool SideCollision { get; set; }
    public Vector2 HitNormal { get; set; }
    public int portalTouched = 0;

    private bool verticalGravity;

    public void Init(GameObject entityGO) {
        _collisionMask = LayerMask.GetMask("PortalPlatform", "Platform");
        _portalMask = LayerMask.GetMask("Portal");
        _buttonMask = LayerMask.GetMask("Button");
        _collider = entityGO.GetComponent<BoxCollider2D>();
    }

    public Vector2 Move(Vector2 moveAmount, GameObject entityGO) {
        float deltaX = moveAmount.x;
        float deltaY = moveAmount.y;
        Vector2 entityPosition = entityGO.transform.position;
        var gravity = GetComponent<gravityHandler>();
        gravityHandler.GravityOrientation gravityOr = gravity.GetGravityOrientation();

        if (gravityOr == gravityHandler.GravityOrientation.Up || gravityOr == gravityHandler.GravityOrientation.Down)
            verticalGravity = true;
        else
            verticalGravity = false;

        //touchingBox = false;
        //inDoor = false;

        //Resolve any possible collisions below and above the entity
        if (deltaY != 0 || verticalGravity)
            deltaY = YAxisCollisions(deltaY, Mathf.Sign(deltaX), entityPosition);
        //Resolve any possible collisions left and right of the entity
        //Check if our deltaX value is 0 to avoid unnecessary collision checks
        if (deltaX != 0 || !verticalGravity)
            deltaX = XAxisCollisions(deltaX, entityPosition);
        //If moving sideways in air, resolve any possible diagonal collisions
        if (deltaX != 0 && deltaY != 0 && !SideCollision && !onGround) {
            DiagonalCollisions(ref deltaX, ref deltaY, entityPosition);
        }
        Vector2 finalTransform = new Vector2(deltaX, deltaY);
        return finalTransform;
    }

    private float XAxisCollisions(float deltaX, Vector2 entityPosition) {
        if (verticalGravity)
            SideCollision = false;
        else
            onGround = false;

        _collisionRect = GetNewCollisionRect();

        float margin = 0.04f;
        int numOfRays = 2;
        int portalHits = 0;
        int missedButton = 0;
        Vector2 rayStartPoint = new Vector2(_collisionRect.center.x, _collisionRect.yMin + margin);
        Vector2 rayEndPoint = new Vector2(_collisionRect.center.x, _collisionRect.yMax - margin);
        float distance = (_collisionRect.width / 2) + Mathf.Abs(deltaX);

        Vector2 rayDirection = new Vector2(Mathf.Sign(deltaX), 0);
        float xCandidate = -1f;

        for (int i = 0; i < numOfRays; ++i) {
            float lerpAmount = (float) i / ((float) numOfRays - 1);
            Vector2 origin = Vector2.Lerp(rayStartPoint, rayEndPoint, lerpAmount);
            
            // non-collision rays will render white for debugging
            Debug.DrawRay(origin, rayDirection, Color.white);

            RaycastHit2D hit = Physics2D.Raycast(origin, rayDirection, distance, _collisionMask);
            RaycastHit2D hitPortal = Physics2D.Raycast(origin, rayDirection, distance, _portalMask);

            if (hitPortal) {
                var portalScript = GetComponent<PortalScript>();
                if (hitPortal.collider.gameObject == portalScript.Portal1 && portalScript.Portal2.activeSelf) {
                    portalHits++;
                    if (portalHits == numOfRays) {
                        portalTouched = 1;
                        touchedPortal = true;
                        onGround = false;
                        xCandidate = -1;
                        break;
                    }
                }
                else if (hitPortal.collider.gameObject == portalScript.Portal2 && portalScript.Portal1.activeSelf) {
                    portalHits++;
                    if (portalHits == numOfRays) {
                        portalTouched = 2;
                        touchedPortal = true;
                        onGround = false;
                        xCandidate = -1;
                        break;
                    }
                }
            }
            if (hit) {
                HitNormal = hit.normal;
                // collision rays render yellow for debugging
                Debug.DrawRay(origin, rayDirection, Color.yellow);
                // find the x edge which is in the direction of travel, and assign it to float x
                float x = Mathf.Sign(deltaX) == -1 ? _collisionRect.xMin : _collisionRect.xMax;

                //deltaX = (_collisionRect.center.x + hit.distance * rayDirection.x - x) + xSkinSpace;
                float checkX = (_collisionRect.center.x + hit.distance * rayDirection.x - x) + xSkinSpace;
                if (xCandidate == -1 || Mathf.Abs(checkX) < Mathf.Abs(xCandidate)) {
                    xCandidate = Mathf.Abs(checkX);
                }
                if (xCandidate < 0.1)
                    xCandidate = 0;

                if (verticalGravity)
                    SideCollision = true;
                else
                    onGround = true;
                touchedPortal = false;
            }
            // for horizontal gravity, check for button pushes
            if (!verticalGravity) {
                RaycastHit2D hitButton = Physics2D.Raycast(origin, rayDirection, distance, _buttonMask);
                //Button collision detection works in tandem with buttonScript
                if (hitButton && !pushedButton) {// !SideCollision && !pushedButton) {
                    enteredButton = true;
                    pushedButton = true;
                }
                if (!hitButton) {
                    missedButton++;
                }
                if (pushedButton && (missedButton == numOfRays)) { //all rays missed
                                                                   //Jumped of the button
                    exitedButton = true;
                    pushedButton = false;
                }
            }
        }
        // if something was detected
        if (xCandidate > -1) {
            deltaX = xCandidate * Mathf.Sign(deltaX);
        }


        return deltaX;
    }

    private float YAxisCollisions(float deltaY, float dirX, Vector3 entityPosition) {
        if (verticalGravity)
            onGround = false;
        else
            SideCollision = false;
        
        _collisionRect = GetNewCollisionRect();

        float margin = 0.04f;
        int numOfRays = 2;
        int portalHits = 0;
        int missedButton = 0;
        Vector2 rayStartPoint = new Vector2(_collisionRect.xMin + margin, _collisionRect.center.y);
        Vector2 rayEndPoint = new Vector2(_collisionRect.xMax - margin, _collisionRect.center.y);
        float distance = (_collisionRect.width / 2) + Mathf.Abs(deltaY);

        Vector2 rayDirection = new Vector2(0, Mathf.Sign(deltaY));
        float yCandidate = -1f;

        for (int i = 0; i < numOfRays; ++i) {
            float lerpAmount = (float)i / ((float)numOfRays - 1);
            Vector2 origin = dirX == -1 ? Vector2.Lerp(rayStartPoint, rayEndPoint, lerpAmount)
                                        : Vector2.Lerp(rayEndPoint, rayStartPoint, lerpAmount);

            // non-collision rays will render white for debugging
            Debug.DrawRay(origin, rayDirection, Color.white);

            RaycastHit2D hit = Physics2D.Raycast(origin, rayDirection, distance, _collisionMask);
            RaycastHit2D hitPortal = Physics2D.Raycast(origin, rayDirection, distance, _portalMask);

            if (hitPortal) {
                var portalScript = GetComponent<PortalScript>();
                if (hitPortal.collider.gameObject == portalScript.Portal1 && portalScript.Portal2.activeSelf) {
                    portalHits++;
                    if (portalHits == numOfRays) {
                        portalTouched = 1;
                        touchedPortal = true;
                        yCandidate = -1;
                        break;
                    }
                }
                else if (hitPortal.collider.gameObject == portalScript.Portal2 && portalScript.Portal1.activeSelf) {
                    portalHits++;
                    if (portalHits == numOfRays) {
                        portalTouched = 2;
                        touchedPortal = true;
                        yCandidate = -1;
                        break;
                    }
                }
            }
            if (hit) {
                HitNormal = hit.normal;
                // collision rays will render yellow for debugging
                Debug.DrawRay(origin, rayDirection, Color.yellow);
                // find the y edge which is in the direction of travel, and assign it to float y
                float y = Mathf.Sign(deltaY) == -1 ? _collisionRect.yMin : _collisionRect.yMax;

                //deltaY = (_collisionRect.center.y + hit.distance * rayDirection.y - y) + ySkinSpace;
                float checkY = (_collisionRect.center.y + hit.distance * rayDirection.y - y) + ySkinSpace;
                if (yCandidate == -1 || Mathf.Abs(checkY) < Mathf.Abs(yCandidate)) {
                    yCandidate = Mathf.Abs(checkY);
                }
                if (yCandidate < 0.1)
                    yCandidate = 0;

                if (verticalGravity)
                    onGround = true;
                else
                    SideCollision = true;
                touchedPortal = false;
            }
            // if gravity is vertical, y axis checks for button pushes
            if (verticalGravity) {
                RaycastHit2D hitButton = Physics2D.Raycast(origin, rayDirection, distance, _buttonMask);
                //Button collision detection works in tandem with buttonScript
                if (hitButton && !pushedButton) {// !SideCollision && !pushedButton) {
                    enteredButton = true;
                    pushedButton = true;
                }
                if (!hitButton) {
                    missedButton++;
                }
                if (pushedButton && (missedButton == numOfRays)) { //all rays missed
                                                                   //Jumped of the button
                    exitedButton = true;
                    pushedButton = false;
                }
            }
        }
        // if something was detected
        if (yCandidate > -1) {
            deltaY = yCandidate * Mathf.Sign(deltaY);
        }

        return deltaY;
    }

    private void DiagonalCollisions(ref float deltaX, ref float deltaY, Vector2 entityPosition) {
        _collisionRect = GetNewCollisionRect();
        Vector2 oldDirection = new Vector2(Mathf.Sign(deltaX), Mathf.Sign(deltaY));

        Vector2 direction = new Vector2(deltaX, deltaY);
        Vector2 origin = _collisionRect.center + new Vector2((_collisionRect.height / 2) * Mathf.Sign(deltaX), (_collisionRect.height / 2) * Mathf.Sign(deltaY));
        float distance = direction.magnitude;
        
        // white raycast is old raycast, red one is new raycast
        Debug.DrawRay(_collisionRect.center, oldDirection, Color.white);
        Debug.DrawRay(origin, direction, Color.red);

        RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, _collisionMask);

        if (hit) {
            HitNormal = hit.normal;
            Debug.DrawRay(_collisionRect.center, direction, Color.yellow);
            // Stop deltaX and let entity drop by deltaY
            if (verticalGravity)
                deltaX = 0;
            else
                deltaY = 0;

            SideCollision = true;
        }
    }

    private Rect GetNewCollisionRect() {
    return new Rect(
        _collider.bounds.min.x,
        _collider.bounds.min.y,
        _collider.bounds.size.x,
        _collider.bounds.size.y);
    }
}
