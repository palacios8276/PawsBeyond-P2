using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEditor;

public static class P2Phase1BathroomSmoke
{
    static void Check(bool ok, string label)
    { if (!ok) throw new Exception("FAIL: " + label); Debug.Log("P2 Fase 1 PASS: " + label); }
    static async Task Until(Func<bool> condition, string label, int seconds = 15)
    {
        for (int i = 0; i < seconds * 20 && !condition(); i++) await Task.Delay(50);
        Check(condition(), label);
    }
    static void Place(CharacterController cc, Vector3 position)
    {
        cc.enabled = false; cc.transform.position = position; cc.enabled = true; Physics.SyncTransforms();
    }
    static async Task Walk(CharacterController cc, Vector3 target)
    {
        for (int i = 0; i < 500; i++)
        {
            Vector3 d = target - cc.transform.position; d.y = 0;
            if (d.magnitude < 0.08f) return;
            cc.Move(Vector3.ClampMagnitude(d, 0.05f)); await Task.Delay(20);
        }
        throw new Exception("Paso obstruido: " + cc.transform.position + " hacia " + target);
    }
    static async Task E(Keyboard keyboard)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); await Task.Delay(160);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); await Task.Delay(160);
    }
    public static async Task<string> Main()
    {
        Check(Application.isPlaying, "Play activo");
        bool bg = Application.runInBackground; Application.runInBackground = true;
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>("P2Phase1_VirtualKeyboard"); keyboard.MakeCurrent();
        try
        {
            int frame = Time.frameCount; await Task.Delay(250); Check(Time.frameCount > frame, "frames avanzan");
            var player = GameObject.Find("Player"); var cc = player.GetComponent<CharacterController>();
            var interaction = player.GetComponent<PlayerInteraction>();
            var puzzle = GameObject.Find("BathroomEntryTrigger").GetComponent<BathroomBasketDrop>();
            var journey = GameObject.Find("Owners").GetComponent<OwnersJourney>();
            var hinge = GameObject.Find("BathroomDoor").GetComponent<HingeJoint>();
            var door = GameObject.Find("BathroomDoorTrigger").GetComponent<DoorTriggerDetector>();
            Check(cc && !player.GetComponent<Rigidbody>(), "CharacterController sin Rigidbody");
            Check(!journey.HasStarted, "preparación antes de iniciar recorrido");
            var settings = new SerializedObject(journey);
            settings.FindProperty("maximumStartWait").floatValue = 3f;
            settings.FindProperty("moveSpeed").floatValue = 8f;
            settings.FindProperty("observationTime").floatValue = 100f;
            settings.ApplyModifiedPropertiesWithoutUndo();
            // Preparación de caso: el perro abandona rápidamente el dormitorio y entra primero.
            Place(cc, new Vector3(18.3f, 0.15f, -0.21f)); await Task.Delay(500);
            Check(!puzzle.IsActivated, "INC-01: perro primero no activa el baño");
            await Until(() => journey.HasStarted, "INC-02: inicia aun sin completar observación", 6);
            await Until(() => puzzle.IsReady, "INC-01: llegada real de dueños activa sin reentrada", 45);
            Check(journey.WaitingForBathroom, "dueños esperan el solve");
            Check(puzzle.Clothes.Length == 3 && !puzzle.IsSolved, "tres prendas iniciales");
            Check(door.IsPuzzleLocked && Mathf.Abs(hinge.angle) < 2f, "bisagra cerrada y bloqueada");
            var poses = puzzle.Clothes.Select(c => c.position).ToArray();
            var rotations = puzzle.Clothes.Select(c => c.rotation).ToArray();
            // Prendas apiladas mediante preparación de prueba; movimiento del perro con CharacterController.Move.
            for (int i = 0; i < 3; i++)
            { puzzle.Clothes[i].position = new Vector3(14.7f, 0.3f + i * 0.45f, 0f); puzzle.Clothes[i].linearVelocity = Vector3.zero; }
            int probes = 0;
            foreach (float y in new[] { 0.15f, 0.65f, 1.15f, 1.5f })
            foreach (float z in new[] { -1.9f, -1.45f, -0.7f, 0f, 0.7f, 1.3f, 1.8f })
            {
                Place(cc, new Vector3(15.35f, y, z));
                for (int k = 0; k < 40; k++) { cc.Move(Vector3.left * 0.075f); await Task.Delay(10); }
                Check(cc.transform.position.x > 13.45f, "INC-04/05: acceso bloqueado altura=" + y + " lateral=" + z);
                Check(Mathf.Abs(hinge.angle) < 2f, "INC-03: cierre estable bajo empuje"); probes++;
            }
            Place(cc, new Vector3(18.3f, 0.15f, -0.21f));
            for (int i = 0; i < 3; i++)
            { puzzle.Clothes[i].position = poses[i]; puzzle.Clothes[i].rotation = rotations[i]; puzzle.Clothes[i].linearVelocity = Vector3.zero; puzzle.Clothes[i].angularVelocity = Vector3.zero; }
            await Task.Delay(800);
            Check(!puzzle.IsSolved && !puzzle.IsBasketRemoved, "física sin tres agarres no resuelve");
            int firstId = puzzle.Clothes[0].GetInstanceID();
            for (int i = 0; i < 3; i++)
            {
                float z = puzzle.Clothes[i].position.z;
                await Walk(cc, new Vector3(18.3f, 0.15f, -1.65f));
                await Walk(cc, new Vector3(15.4f, 0.15f, -1.65f));
                await Walk(cc, new Vector3(15.4f, 0.15f, z));
                player.transform.rotation = Quaternion.LookRotation(Vector3.left);
                await E(keyboard);
                Check(interaction.IsDragging && puzzle.Clothes[i].GetComponent<BasketDrag>().IsHeld, "E agarra prenda " + i);
                await Walk(cc, new Vector3(18.3f, 0.15f, z));
                await Until(() => puzzle.Clothes[i].position.x > 16.7f, "prenda retirada " + i);
                await E(keyboard); Check(!interaction.IsDragging, "E suelta prenda " + i);
                if (i < 2) Check(!puzzle.IsSolved && door.IsPuzzleLocked, "menos de tres prendas no liberan puerta");
            }
            await Until(() => puzzle.IsSolved && puzzle.RemovedClothesCount == 3, "tres prendas resuelven");
            await Until(() => puzzle.IsBasketRemoved, "cesto retirado solo después de solve");
            Check(!door.IsPuzzleLocked, "restaura límites físicos de bisagra");
            await Walk(cc, new Vector3(18.3f, 0.15f, -1.65f));
            await Walk(cc, new Vector3(15.4f, 0.15f, -1.65f));
            await Walk(cc, new Vector3(15.4f, 0.15f, -0.21f));
            await Until(() => Mathf.Abs(hinge.angle) > 75f, "puerta abre físicamente después de resolver");
            await Walk(cc, new Vector3(11.1f, 0.15f, -0.21f));
            await Walk(cc, new Vector3(18.3f, 0.15f, -0.21f));
            Check(puzzle.IsSolved && puzzle.Clothes[0].GetInstanceID() == firstId, "reentrada no duplica ni reinicia");
            await Until(() => journey.IsComplete, "recorrido P1 continúa hasta cocina", 30);
            return "PASS automático: entrada temprana, inicio sin observación, " + probes + " intentos de escape por altura/lateral, cierre estable, E con 3 prendas, apertura, reentrada y continuación. Preparación: velocidad de dueños 8 m/s y posiciones controladas solo en Play; controles virtuales, no prueba manual.";
        }
        finally { InputSystem.RemoveDevice(keyboard); Application.runInBackground = bg; }
    }
}

