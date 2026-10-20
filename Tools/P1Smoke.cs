using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class P1Smoke
{
    private static void Check(bool ok, string message)
    {
        if (!ok) throw new Exception("FAIL: " + message);
        Debug.Log("PASS: " + message);
    }
    private static async Task Wait(Func<bool> condition, string message)
    {
        for (int i = 0; i < 160 && !condition(); i++) await Task.Delay(50);
        Check(condition(), message);
    }
    private static async Task E()
    {
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.E));
        await Task.Delay(150);
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
        await Task.Delay(150);
    }
    private static async Task Walk(CharacterController cc, Vector3 target)
    {
        for (int i = 0; i < 500; i++)
        {
            var delta = target - cc.transform.position;
            delta.y = 0f;
            if (delta.magnitude < 0.06f) return;
            cc.Move(Vector3.ClampMagnitude(delta, 0.035f));
            await Task.Delay(20);
        }
        throw new Exception("Walk obstructed at " + cc.transform.position + " toward " + target);
    }
    public static async Task<string> Main()
    {
        bool background = Application.runInBackground;
        try { Application.runInBackground = true; return await Run(); }
        finally
        {
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            Application.runInBackground = background;
        }
    }
    private static async Task<string> Run()
    {
        Check(Application.isPlaying, "Play mode");
        var player = GameObject.Find("Player");
        var cc = player.GetComponent<CharacterController>();
        var interaction = player.GetComponent<PlayerInteraction>();
        var puzzle = GameObject.Find("BathroomEntryTrigger").GetComponent<BathroomBasketDrop>();
        var hinge = GameObject.Find("BathroomDoor").GetComponent<HingeJoint>();
        Check(cc && !player.GetComponent<Rigidbody>(), "CharacterController without Rigidbody");
        Check(!puzzle.IsActivated && !puzzle.IsSolved, "fresh puzzle");
        Check(puzzle.Clothes.Length == 3, "three initial clothes instantiated inside basket");
        foreach(var cloth in puzzle.Clothes)
        {
            Vector3 local=puzzle.ActiveBasket.transform.InverseTransformPoint(cloth.position);
            Check(Mathf.Abs(local.x)<0.4f && Mathf.Abs(local.z)<0.9f && local.y>0f && local.y<0.4f
                && cloth.isKinematic, "initial cloth contained above basket base");
        }
        Vector3 initial = player.transform.position;
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.W));
        await Wait(() => Vector3.Distance(initial, player.transform.position) > 0.4f, "New Input movement");
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
        await Task.Delay(150);
        if (player.transform.position.x < -12f)
        {
            await Walk(cc, new Vector3(-14f, 0.15f, -1.15f));
            var bedroomHinge = GameObject.Find("Door").GetComponent<HingeJoint>();
            await Wait(() => bedroomHinge.angle < -70f, "bedroom physical door opens");
            await Walk(cc, new Vector3(-8f, 0.15f, -1.15f));
            await Walk(cc, new Vector3(0f, 0.15f, -0.21f));
            await Walk(cc, new Vector3(11.1f, 0.15f, -0.21f));
            Check(Camera.main && Vector3.Distance(Camera.main.transform.position, player.transform.position) < 8f,
                "third-person camera follows bedroom and hall circulation");
        }
        await E();
        Check(!interaction.IsDragging, "out-of-range E ignored");
        await Walk(cc, new Vector3(12.1f, 0.15f, -0.21f));
        await Wait(() => hinge.angle < -70f, "door opens before entry");
        await Walk(cc, new Vector3(17.3f, 0.15f, -0.21f));
        await Wait(() => puzzle.IsReady, "entry event closes door and drops laundry");
        Check(puzzle.Clothes.Length == 3, "three clothes instantiated from prefab");
        Check(puzzle.ActiveBasket.position.y > 0.2f && puzzle.ActiveBasket.position.y < 0.6f,
            "basket rests on floor");
        foreach(var cloth in puzzle.Clothes)
        {
            Vector3 local=puzzle.ActiveBasket.transform.InverseTransformPoint(cloth.position);
            Check(Mathf.Abs(local.x)<0.45f && Mathf.Abs(local.z)<0.95f && local.y>0f && local.y<0.4f,
                "clothes fell with basket and remain inside/on it");
        }
        Check(!puzzle.IsBasketRemoved && puzzle.ActiveBasket.gameObject.activeSelf, "ray cannot remove unresolved basket");
        await Walk(cc, new Vector3(15.4f, 0.15f, -0.21f));
        await Task.Delay(1500);
        Check(hinge.spring.targetPosition < -80f && hinge.angle > -60f && !puzzle.IsSolved,
            "clothes physically block requested door opening");
        int firstId = puzzle.Clothes[0].GetInstanceID();
        for (int i = 0; i < puzzle.Clothes.Length; i++)
        {
            var cloth = puzzle.Clothes[i];
            float z = cloth.position.z;
            await Walk(cc, new Vector3(15.4f, 0.15f, z));
            player.transform.rotation = Quaternion.LookRotation(Vector3.left);
            await E();
            Check(interaction.IsDragging && cloth.GetComponent<BasketDrag>().IsHeld, "E raycast grabs cloth " + i);
            if(i==0)
            {
                Vector3 before=player.transform.position;
                InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.S));
                await Task.Delay(300);
                InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
                await Task.Delay(100);
                Check(Vector3.Distance(before,player.transform.position)>0.2f && interaction.IsDragging,
                    "original WASD works while carrying");
            }
            await Walk(cc, new Vector3(18.3f, 0.15f, z));
            await Wait(() => cloth.position.x > 16.7f, "cloth physically removed " + i);
            await E();
            Check(!interaction.IsDragging, "E releases cloth " + i);
            if (i < 2)
            {
                Check(!puzzle.IsSolved, "remaining clothes keep puzzle blocked");
                Check(!puzzle.IsBasketRemoved, "basket stays until third cloth removed");
                await Walk(cc, new Vector3(18.3f, 0.15f, -1.65f));
                await Walk(cc, new Vector3(15.4f, 0.15f, -1.65f));
            }
        }
        await Wait(() => puzzle.IsSolved, "removing clothes solves puzzle");
        Check(puzzle.BathroomPuzzleSolved && puzzle.RemovedClothesCount==3, "BathroomPuzzleSolved after all three clothes");
        await Wait(() => puzzle.IsBasketRemoved, "post-solution ray abducts basket");
        Check(!puzzle.ActiveBasket.gameObject.activeSelf, "basket removed from scene");
        await Walk(cc, new Vector3(18.3f, 0.15f, -1.65f));
        await Walk(cc, new Vector3(15.4f, 0.15f, -1.65f));
        await Walk(cc, new Vector3(15.4f, 0.15f, -0.21f));
        await Wait(() => hinge.angle < -75f, "door physically opens after removal");
        await Walk(cc, new Vector3(11.1f, 0.15f, -0.21f));
        Check(player.transform.position.x < 12f, "player exits physically");
        await Walk(cc, new Vector3(17.3f, 0.15f, -0.21f));
        Check(puzzle.IsSolved && puzzle.Clothes.Length == 3 && puzzle.Clothes[0].GetInstanceID() == firstId,
            "re-entry neither respawns nor resets puzzle");
        await Walk(cc, new Vector3(11.1f, 0.15f, -0.21f));
        await Walk(cc, new Vector3(5f, 0.15f, -9f));
        await Walk(cc, new Vector3(5f, 0.15f, -12f));
        await Walk(cc, new Vector3(2.4f, 0.15f, -13f));
        await Walk(cc, new Vector3(2.4f, 0.15f, -17.5f));
        await Walk(cc, new Vector3(2.4f, 0.15f, -12f));
        await Walk(cc, new Vector3(5f, 0.15f, -12f));
        await Walk(cc, new Vector3(5f, 0.15f, -9f));
        await Walk(cc, new Vector3(3f, 0.15f, 0f));
        await Walk(cc, new Vector3(3f, 0.15f, 8f));
        Check(Camera.main && Vector3.Distance(Camera.main.transform.position, player.transform.position) < 8f,
            "camera follows kitchen and final living room circulation");
        Check(GameObject.Find("Owner_A_Capsule").GetComponent<OwnerFloat>()
            && GameObject.Find("Owner_B_Capsule").GetComponent<OwnerFloat>(), "both owners use simple float scripts");
        return "PASS P1QA loaded basket, E removal, post-solution abduction, free door and full room route";
    }
}
