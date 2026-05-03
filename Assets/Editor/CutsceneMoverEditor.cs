using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CutsceneMover))]
public class CutsceneMoverEditor : Editor
{
    readonly List<bool> stepFoldouts = new();
    readonly List<bool> extraFoldouts = new();

    SerializedProperty playOnAwakeProp;
    SerializedProperty loopProp;
    SerializedProperty forwardReferenceProp;
    SerializedProperty animatorProp;
    SerializedProperty fadeCanvasGroupProp;
    SerializedProperty onSequenceFinishedProp;

    SerializedProperty driverProp;
    SerializedProperty characterProp;
    SerializedProperty rbProp;
    SerializedProperty stepsProp;

    GUIStyle cardTitleStyle;
    GUIStyle cardSummaryStyle;

    void OnEnable()
    {
        playOnAwakeProp = serializedObject.FindProperty("playOnAwake");
        loopProp = serializedObject.FindProperty("loop");
        forwardReferenceProp = serializedObject.FindProperty("forwardReference");
        animatorProp = serializedObject.FindProperty("animator");
        fadeCanvasGroupProp = serializedObject.FindProperty("fadeCanvasGroup");
        onSequenceFinishedProp = serializedObject.FindProperty("OnSequenceFinished");

        driverProp = serializedObject.FindProperty("driver");
        characterProp = serializedObject.FindProperty("character");
        rbProp = serializedObject.FindProperty("rb");
        stepsProp = serializedObject.FindProperty("steps");

        EnsureFoldoutCount();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EnsureStyles();
        EnsureFoldoutCount();

        var mover = (CutsceneMover)target;

        DrawIntro(mover);
        EditorGUILayout.Space(6f);
        DrawGeneralSection();
        EditorGUILayout.Space(6f);
        DrawDriverSection();
        EditorGUILayout.Space(6f);
        DrawPlaybackSection(mover);
        EditorGUILayout.Space(6f);
        DrawStepToolbar();
        EditorGUILayout.Space(6f);
        DrawStepList(mover);
        EditorGUILayout.Space(6f);
        DrawMentalModelBox();

        serializedObject.ApplyModifiedProperties();
        if (GUI.changed)
        {
            SceneView.RepaintAll();
        }

        if (Application.isPlaying)
        {
            Repaint();
        }
    }

    void OnSceneGUI()
    {
        var mover = (CutsceneMover)target;
        if (mover.steps == null || mover.steps.Count == 0) return;

        Vector3 currentPos = mover.transform.position;
        Quaternion currentRotation = mover.transform.rotation;

        Handles.color = new Color(0.25f, 0.85f, 1f, 1f);
        Handles.SphereHandleCap(0, currentPos, Quaternion.identity, 0.2f, EventType.Repaint);
        Handles.Label(currentPos + Vector3.up * 0.35f, "Start");

        for (int i = 0; i < mover.steps.Count; i++)
        {
            Step step = mover.steps[i];
            if (step == null) continue;

            if (step.skip)
            {
                Handles.color = new Color(0.7f, 0.7f, 0.7f, 0.65f);
                Handles.Label(currentPos + Vector3.up * (0.55f + i * 0.03f), $"{i + 1}. Skip");
                continue;
            }

            switch (step.type)
            {
                case StepType.Move:
                    DrawMovePreview(mover, step, i, ref currentPos, ref currentRotation);
                    break;
                case StepType.Rotate:
                    DrawRotatePreview(step, i, currentPos, ref currentRotation);
                    break;
                case StepType.Wait:
                    Handles.color = new Color(1f, 0.75f, 0.2f, 1f);
                    Handles.Label(currentPos + Vector3.up * (0.45f + i * 0.02f), $"{i + 1}. Wait {step.waitSeconds:0.##}s");
                    break;
                case StepType.PlayAnimation:
                    Handles.color = new Color(0.7f, 0.9f, 0.3f, 1f);
                    Handles.Label(currentPos + Vector3.up * (0.45f + i * 0.02f), $"{i + 1}. Anim {SafeLabel(step.animParam, "param")}");
                    break;
                case StepType.Fade:
                    Handles.color = new Color(0.05f, 0.05f, 0.05f, 1f);
                    Handles.Label(currentPos + Vector3.up * (0.45f + i * 0.02f), $"{i + 1}. {Nicify(step.fadeMode)}");
                    break;
                case StepType.Teleport:
                    DrawTeleportPreview(step, i, ref currentPos);
                    break;
                case StepType.Attach:
                    DrawAttachPreview(step, i, currentPos);
                    break;
                case StepType.InvokeEvent:
                    Handles.color = new Color(1f, 0.45f, 0.45f, 1f);
                    Handles.Label(currentPos + Vector3.up * (0.45f + i * 0.02f), $"{i + 1}. Event");
                    break;
            }
        }
    }

    void DrawIntro(CutsceneMover mover)
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Cutscene Mover", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Bu editor step'leri turune gore ayirir ve gereksiz alanlari gizler. Move stepinde 'Speed' yurume hizidir. 'Turn Speed' sadece hareketten once yone donmeyi etkiler. Rotate stepinde hiz degil 'Duration' kullanilir.",
                MessageType.Info);

            if (mover.steps == null || mover.steps.Count == 0)
            {
                EditorGUILayout.HelpBox("Henuz hic step yok. Asagidaki hizli ekleme butonlariyla baslayabilirsin.", MessageType.None);
            }

            if (Application.isPlaying)
            {
                if (mover.IsPlaying && mover.CurrentStepIndex >= 0)
                {
                    EditorGUILayout.HelpBox($"Play Mode: su an Step {mover.CurrentStepIndex + 1} calisiyor.", MessageType.None);
                }
                else
                {
                    EditorGUILayout.HelpBox("Play Mode'da her step kartindaki 'Test' ve 'From Here' butonlari aktif olur.", MessageType.None);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Test butonlari Play Mode'da aktif olur. Edit Mode'da once duzenleyip sonra Play Mode'da tek tek deneyebilirsin.", MessageType.None);
            }
        }
    }

    void DrawGeneralSection()
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(playOnAwakeProp, new GUIContent("Play On Awake", "Sahne acildiginda sequence otomatik baslar."));
            EditorGUILayout.PropertyField(loopProp, new GUIContent("Loop", "Tum step'ler bitince basa doner."));
            EditorGUILayout.PropertyField(forwardReferenceProp, new GUIContent("Forward Reference", "Move direction bunun yon uzayina gore hesaplanir. Bos kalirsa obje kendi forward'unu kullanir."));
            EditorGUILayout.PropertyField(animatorProp, new GUIContent("Animator", "Animation step'lerinde kullanilacak animator."));
            EditorGUILayout.PropertyField(fadeCanvasGroupProp, new GUIContent("Fade Canvas Group", "Tam ekran siyah image uzerindeki CanvasGroup. Fade step bu alpha degerini animeler."));
            EditorGUILayout.PropertyField(onSequenceFinishedProp, new GUIContent("On Sequence Finished", "Tum sequence bittiginde bir kez cagrilir."));

            EditorGUILayout.HelpBox("Forward Reference genelde kamera, araba govdesi veya baska bir referans transform ise faydali olur. Bos birakirsan step yonleri objenin kendi bakisina gore hesaplanir.", MessageType.None);
            if (fadeCanvasGroupProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Fade step kullanacaksan bir Canvas icine tam ekran siyah Image koyup ona CanvasGroup ekle. Sonra buraya o CanvasGroup'u ver.", MessageType.None);
            }
        }
    }

    void DrawDriverSection()
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Driver", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(driverProp, new GUIContent("Move Driver", "Pozisyon degisikligini hangi sistem uzerinden uygulayacagini sec."));

            DriverType driver = (DriverType)driverProp.enumValueIndex;
            switch (driver)
            {
                case DriverType.Transform:
                    EditorGUILayout.HelpBox("En basit mod. Objeyi direkt transform.position ile tasir.", MessageType.None);
                    break;

                case DriverType.CharacterController:
                    EditorGUILayout.PropertyField(characterProp, new GUIContent("Character Controller", "CharacterController.Move kullanilir."));
                    if (characterProp.objectReferenceValue == null)
                    {
                        EditorGUILayout.HelpBox("CharacterController driver secili ama referans bos. Script fallback olarak transform ile tasir.", MessageType.Warning);
                    }
                    break;

                case DriverType.Rigidbody:
                    EditorGUILayout.PropertyField(rbProp, new GUIContent("Rigidbody", "Rigidbody.MovePosition kullanilir."));
                    if (rbProp.objectReferenceValue == null)
                    {
                        EditorGUILayout.HelpBox("Rigidbody driver secili ama referans bos. Script fallback olarak transform ile tasir.", MessageType.Warning);
                    }
                    break;
            }
        }
    }

    void DrawPlaybackSection(CutsceneMover mover)
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Quick Controls", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!Application.isPlaying))
                {
                    if (GUILayout.Button("Play All"))
                    {
                        serializedObject.ApplyModifiedProperties();
                        mover.Play();
                        GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button("Stop"))
                    {
                        mover.Stop();
                        GUIUtility.ExitGUI();
                    }
                }

                if (GUILayout.Button("Expand All"))
                {
                    SetAllFoldouts(true);
                }

                if (GUILayout.Button("Collapse All"))
                {
                    SetAllFoldouts(false);
                }
            }
        }
    }

    void DrawStepToolbar()
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Step Tools", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Hizli ekleme butonlari yeni step'i mantikli varsayilanlarla olusturur. Her kartta ayri not, ozet, duplicate ve test butonlari var.", MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Move")) AddStep(StepType.Move);
                if (GUILayout.Button("Add Rotate")) AddStep(StepType.Rotate);
                if (GUILayout.Button("Add Wait")) AddStep(StepType.Wait);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Anim")) AddStep(StepType.PlayAnimation);
                if (GUILayout.Button("Add Fade")) AddStep(StepType.Fade);
                if (GUILayout.Button("Add Teleport")) AddStep(StepType.Teleport);
                if (GUILayout.Button("Add Attach")) AddStep(StepType.Attach);
                if (GUILayout.Button("Add Event")) AddStep(StepType.InvokeEvent);
            }
        }
    }

    void DrawStepList(CutsceneMover mover)
    {
        if (stepsProp.arraySize == 0) return;

        EditorGUILayout.LabelField("Steps", EditorStyles.boldLabel);

        for (int i = 0; i < stepsProp.arraySize; i++)
        {
            SerializedProperty stepProp = stepsProp.GetArrayElementAtIndex(i);
            DrawStepCard(mover, stepProp, i);
            EditorGUILayout.Space(4f);
        }
    }

    void DrawStepCard(CutsceneMover mover, SerializedProperty stepProp, int index)
    {
        SerializedProperty skipProp = stepProp.FindPropertyRelative("skip");

        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                bool wantedFoldout = EditorGUILayout.Foldout(stepFoldouts[index], BuildStepHeader(stepProp, index), true, cardTitleStyle);
                if (wantedFoldout != stepFoldouts[index])
                {
                    if (wantedFoldout)
                    {
                        SetAllFoldouts(false);
                        stepFoldouts[index] = true;
                    }
                    else
                    {
                        stepFoldouts[index] = false;
                    }
                }
                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(!Application.isPlaying))
                {
                    if (GUILayout.Button("Test", GUILayout.Width(48f)))
                    {
                        serializedObject.ApplyModifiedProperties();
                        mover.PlaySingleStep(index);
                        GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button("From Here", GUILayout.Width(76f)))
                    {
                        serializedObject.ApplyModifiedProperties();
                        mover.PlayFromStep(index);
                        GUIUtility.ExitGUI();
                    }
                }

                if (GUILayout.Button("Dup", GUILayout.Width(44f)))
                {
                    DuplicateStep(index);
                    GUIUtility.ExitGUI();
                }

                using (new EditorGUI.DisabledScope(index == 0))
                {
                    if (GUILayout.Button("Up", GUILayout.Width(36f)))
                    {
                        MoveStep(index, index - 1);
                        GUIUtility.ExitGUI();
                    }
                }

                using (new EditorGUI.DisabledScope(index >= stepsProp.arraySize - 1))
                {
                    if (GUILayout.Button("Down", GUILayout.Width(48f)))
                    {
                        MoveStep(index, index + 1);
                        GUIUtility.ExitGUI();
                    }
                }

                if (GUILayout.Button("Delete", GUILayout.Width(56f)))
                {
                    DeleteStep(index);
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUILayout.LabelField(BuildStepSummary(stepProp), cardSummaryStyle);

            if (Application.isPlaying && mover.CurrentStepIndex == index && mover.IsPlaying)
            {
                EditorGUILayout.HelpBox("Bu step su an calisiyor.", MessageType.Info);
            }

            if (skipProp.boolValue)
            {
                EditorGUILayout.HelpBox("Bu step skip olarak isaretli. Runtime'da silinmeden atlanir.", MessageType.Warning);
            }

            if (!stepFoldouts[index]) return;

            DrawStepSharedFields(stepProp, index);
            DrawStepTypeFields(stepProp);
        }
    }

    void DrawStepSharedFields(SerializedProperty stepProp, int index)
    {
        EditorGUILayout.Space(2f);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Step Setup", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(stepProp.FindPropertyRelative("type"), new GUIContent("Type", "Bu step'in ne yapacagini belirler."));
                EditorGUILayout.PropertyField(stepProp.FindPropertyRelative("skip"), new GUIContent("Skip", "Silmeden gecici olarak devre disi birakmak icin."));
            }

            EditorGUILayout.PropertyField(
                stepProp.FindPropertyRelative("waitForCompletion"),
                new GUIContent("Wait For Finish", "Aciksa bu step bitene kadar bekler. Kapaliysa step arka planda baslar ve siradaki step hemen calisir."));

            EditorGUILayout.PropertyField(
                stepProp.FindPropertyRelative("startDelay"),
                new GUIContent("Start Delay", "Bu step'in kac saniye sonra baslayacagi. Fade'i yuruyusun ortasinda baslatmak icin kullanisli."));

            if (!stepProp.FindPropertyRelative("waitForCompletion").boolValue)
            {
                EditorGUILayout.HelpBox("Bu step async calisacak. En guvenli kullanim Fade ve bazen Animation stepleridir. Move veya Rotate'i async yaparsan ayni objeyi birden fazla step ayni anda surmeye calisabilir.", MessageType.Warning);
            }

            if (stepProp.FindPropertyRelative("startDelay").floatValue > 0f)
            {
                EditorGUILayout.HelpBox("Bu step belirtilen sure kadar bekleyip sonra baslayacak.", MessageType.None);
            }
        }

        extraFoldouts[index] = EditorGUILayout.Foldout(extraFoldouts[index], "Optional Title / Note", true);
        if (!extraFoldouts[index]) return;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.PropertyField(stepProp.FindPropertyRelative("title"), new GUIContent("Step Title", "Kart basliginda gorunen kisa isim. Bos kalirsa otomatik ozet kullanilir."));
            EditorGUILayout.PropertyField(stepProp.FindPropertyRelative("note"), new GUIContent("Designer Note", "Sadece editor aciklamasidir. Runtime davranisini degistirmez."));
        }
    }

    void DrawStepTypeFields(SerializedProperty stepProp)
    {
        StepType type = (StepType)stepProp.FindPropertyRelative("type").enumValueIndex;

        EditorGUILayout.Space(3f);

        switch (type)
        {
            case StepType.Move:
                DrawMoveFields(stepProp);
                break;
            case StepType.Rotate:
                DrawRotateFields(stepProp);
                break;
            case StepType.Wait:
                DrawWaitFields(stepProp);
                break;
            case StepType.PlayAnimation:
                DrawAnimationFields(stepProp);
                break;
            case StepType.Fade:
                DrawFadeFields(stepProp);
                break;
            case StepType.Teleport:
                DrawTeleportFields(stepProp);
                break;
            case StepType.Attach:
                DrawAttachFields(stepProp);
                break;
            case StepType.InvokeEvent:
                DrawEventFields(stepProp);
                break;
        }
    }

    void DrawMoveFields(SerializedProperty stepProp)
    {
        EditorGUILayout.HelpBox("Move stepi objeyi ilerletir. Speed yurume hizidir. Face Direction aciksa once yone doner. Turn Speed sadece bu on donusu etkiler.", MessageType.None);

        SerializedProperty moveModeProp = stepProp.FindPropertyRelative("moveMode");
        SerializedProperty directionProp = stepProp.FindPropertyRelative("direction");
        SerializedProperty customDirectionProp = stepProp.FindPropertyRelative("customDirection");
        SerializedProperty distanceProp = stepProp.FindPropertyRelative("distance");
        SerializedProperty durationProp = stepProp.FindPropertyRelative("duration");
        SerializedProperty speedProp = stepProp.FindPropertyRelative("speed");
        SerializedProperty easingProp = stepProp.FindPropertyRelative("easing");
        SerializedProperty faceDirectionProp = stepProp.FindPropertyRelative("faceDirection");
        SerializedProperty turnSpeedProp = stepProp.FindPropertyRelative("turnSpeed");

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Direction", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(moveModeProp, new GUIContent("Move Mode", "Mesafeye gore mi yoksa sureye gore mi yuruyecek?"));
            EditorGUILayout.PropertyField(directionProp, new GUIContent("Direction", "Hareketin local yonu."));

            if ((Direction8)directionProp.enumValueIndex == Direction8.Custom)
            {
                EditorGUILayout.PropertyField(customDirectionProp, new GUIContent("Custom Direction", "Sifir vector verirsen step hareket etmez."));
                if (customDirectionProp.vector3Value.sqrMagnitude < 0.0001f)
                {
                    EditorGUILayout.HelpBox("Custom Direction su an sifir. Bu step hareket etmeyecek.", MessageType.Warning);
                }
            }
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Movement", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(speedProp, new GUIContent("Move Speed (m/s)", "Ilerleme hizidir. Donme hizi degildir."));

            MoveMode moveMode = (MoveMode)moveModeProp.enumValueIndex;
            if (moveMode == MoveMode.ByDistance)
            {
                EditorGUILayout.PropertyField(distanceProp, new GUIContent("Distance (m)", "Toplam gidilecek mesafe."));
                if (distanceProp.floatValue <= 0f)
                {
                    EditorGUILayout.HelpBox("Distance 0 veya daha kucuk. Step gorunur ama fiilen hareket etmeyebilir.", MessageType.Warning);
                }
            }
            else
            {
                EditorGUILayout.PropertyField(durationProp, new GUIContent("Duration (sec)", "Ne kadar sure hareket edecegi."));
                EditorGUILayout.HelpBox("ByDuration modunda toplam gidilen mesafe kabaca Speed x Duration kadar olur. Easing secimi hisi degistirir, preview bu mesafeyi yaklasik cizer.", MessageType.None);
                if (durationProp.floatValue <= 0f)
                {
                    EditorGUILayout.HelpBox("Duration 0 veya daha kucuk. Runtime bunu cok kisa bir sureye clamp eder.", MessageType.Warning);
                }
            }

            EditorGUILayout.PropertyField(easingProp, new GUIContent("Easing", "Yavas baslama / yavas bitirme hissi."));
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Facing", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(faceDirectionProp, new GUIContent("Face Direction First", "Yurumeden once yuzunu hareket yonune cevirsin mi?"));

            if (faceDirectionProp.boolValue)
            {
                EditorGUILayout.PropertyField(turnSpeedProp, new GUIContent("Facing Turn Speed", "Buyuk sayi daha hizli yone doner. 0 olursa aninda doner."));
            }
            else
            {
                EditorGUILayout.HelpBox("Face Direction kapali. Bu stepte Turn Speed kullanilmayacak.", MessageType.None);
            }
        }
    }

    void DrawRotateFields(SerializedProperty stepProp)
    {
        EditorGUILayout.HelpBox("Rotate stepi objeyi oldugu yerde dondurur. Burada hiz alani yoktur; ne kadar hizli donecegini Duration belirler.", MessageType.None);

        SerializedProperty rotateModeProp = stepProp.FindPropertyRelative("rotateMode");
        SerializedProperty worldEulerProp = stepProp.FindPropertyRelative("worldEuler");
        SerializedProperty lookTargetProp = stepProp.FindPropertyRelative("lookTarget");
        SerializedProperty durationProp = stepProp.FindPropertyRelative("duration");
        SerializedProperty easingProp = stepProp.FindPropertyRelative("easing");

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(rotateModeProp, new GUIContent("Rotate Mode", "Sabit bir aciya mi donecek, yoksa bir hedefe mi bakacak?"));

            RotateMode rotateMode = (RotateMode)rotateModeProp.enumValueIndex;
            if (rotateMode == RotateMode.WorldEuler)
            {
                EditorGUILayout.PropertyField(worldEulerProp, new GUIContent("World Rotation (Euler)", "Relative degil. Sahnedeki mutlak acidir."));
                EditorGUILayout.HelpBox("Bu alan local degil world rotation'dir. Yani '90 Y' dersen sahnede global 90 dereceye gider.", MessageType.None);
            }
            else
            {
                EditorGUILayout.PropertyField(lookTargetProp, new GUIContent("Look Target", "Objenin bakacagi hedef."));
                if (lookTargetProp.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("LookAtTarget secili ama target bos. Bu step bir sey yapmayacak.", MessageType.Warning);
                }
            }
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Timing", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(durationProp, new GUIContent("Duration (sec)", "Donusun ne kadar surede tamamlanacagi."));
            EditorGUILayout.PropertyField(easingProp, new GUIContent("Easing", "Donus baslangic ve bitis hissi."));
        }
    }

    void DrawWaitFields(SerializedProperty stepProp)
    {
        EditorGUILayout.HelpBox("Wait stepi sadece sure kadar bekler. Karakter yerinde kalir.", MessageType.None);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Timing", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(stepProp.FindPropertyRelative("waitSeconds"), new GUIContent("Wait Seconds", "Bekleme suresi."));
        }
    }

    void DrawAnimationFields(SerializedProperty stepProp)
    {
        EditorGUILayout.HelpBox("Animation stepi secilen Animator parametresini set eder. Istersen hemen sonra bir sure bekletebilirsin.", MessageType.None);

        if (animatorProp.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox("Root'taki Animator bos. Animation step'leri parametre gonderse bile etkisiz kalabilir.", MessageType.Warning);
        }

        SerializedProperty animSetTypeProp = stepProp.FindPropertyRelative("animSetType");
        SerializedProperty animParamProp = stepProp.FindPropertyRelative("animParam");
        SerializedProperty boolValueProp = stepProp.FindPropertyRelative("boolValue");
        SerializedProperty floatValueProp = stepProp.FindPropertyRelative("floatValue");
        SerializedProperty intValueProp = stepProp.FindPropertyRelative("intValue");
        SerializedProperty waitSecondsProp = stepProp.FindPropertyRelative("waitSeconds");

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Parameter", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(animSetTypeProp, new GUIContent("Animator Param Type", "Animator'da hangi tip parametre yazilacak?"));
            EditorGUILayout.PropertyField(animParamProp, new GUIContent("Animator Param Name", "Animator icindeki parametre adi."));

            if (string.IsNullOrWhiteSpace(animParamProp.stringValue))
            {
                EditorGUILayout.HelpBox("Animator Param Name bos. Bu step su an set edecek bir parametre bulamaz.", MessageType.Warning);
            }
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Value", EditorStyles.boldLabel);
            switch ((AnimSetType)animSetTypeProp.enumValueIndex)
            {
                case AnimSetType.Bool:
                    EditorGUILayout.PropertyField(boolValueProp, new GUIContent("Bool Value"));
                    break;
                case AnimSetType.Float:
                    EditorGUILayout.PropertyField(floatValueProp, new GUIContent("Float Value"));
                    break;
                case AnimSetType.Int:
                    EditorGUILayout.PropertyField(intValueProp, new GUIContent("Int Value"));
                    break;
            }
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Timing", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(waitSecondsProp, new GUIContent("Wait After Anim (sec)", "Parametreyi set ettikten sonra ne kadar beklesin?"));
        }
        EditorGUILayout.HelpBox("Eski 'Play Anim At Start' alani runtime'da kullanilmadigi icin gizlendi. Burada gosterilen alanlar gercekten calisan kisimlardir.", MessageType.None);
    }

    void DrawEventFields(SerializedProperty stepProp)
    {
        EditorGUILayout.HelpBox("Event stepi bu noktada UnityEvent cagirir. Kamera degistirme, obje acma, dialog baslatma gibi isler icin kullanabilirsin.", MessageType.None);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Event", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(stepProp.FindPropertyRelative("onInvoke"), new GUIContent("On Invoke"));
        }
    }

    void DrawFadeFields(SerializedProperty stepProp)
    {
        EditorGUILayout.HelpBox("Fade stepi ekrandaki CanvasGroup alpha degerini yavasca degistirir. 0 = gorunmez, 1 = tam siyah.", MessageType.None);

        if (fadeCanvasGroupProp.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox("Root'taki Fade Canvas Group bos. Bu step su an hicbir sey yapmayacak.", MessageType.Warning);
        }

        SerializedProperty fadeModeProp = stepProp.FindPropertyRelative("fadeMode");
        SerializedProperty durationProp = stepProp.FindPropertyRelative("duration");
        SerializedProperty easingProp = stepProp.FindPropertyRelative("easing");

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Fade", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(fadeModeProp, new GUIContent("Fade Type", "Fade Out ekrani siyaha goturur. Fade In siyahi geri acar."));
            EditorGUILayout.PropertyField(durationProp, new GUIContent("Duration (sec)", "Fade'in kac saniyede bitecegi."));
            EditorGUILayout.PropertyField(easingProp, new GUIContent("Easing", "Fade'in yavas baslama / bitis hissi."));
        }

        EditorGUILayout.HelpBox("Ornek: kapida ekran kararsin istiyorsan Fade Out sec. Sonra bir Event veya Attach/Teleport isi yap, sonra ikinci Fade step ile Fade In sec.", MessageType.None);
    }

    void DrawTeleportFields(SerializedProperty stepProp)
    {
        EditorGUILayout.HelpBox("Teleport stepi objeyi tek frame'de world pozisyonuna isinlar. Move'daki custom yon yerine direkt kesin hedef konum vermek istediginde kullan.", MessageType.None);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                stepProp.FindPropertyRelative("teleportWorldPosition"),
                new GUIContent("World Position", "Objenin gidecegi mutlak sahne pozisyonu."));
        }
    }

    void DrawAttachFields(SerializedProperty stepProp)
    {
        EditorGUILayout.HelpBox("Attach stepi baska bir objeyi ele, bagaja ya da herhangi bir target transform'a baglar. En temiz kullanim: el ve bagaj icine bos anchor objeleri koyup onlara attach etmek.", MessageType.None);

        SerializedProperty attachModeProp = stepProp.FindPropertyRelative("attachMode");
        SerializedProperty attachObjectProp = stepProp.FindPropertyRelative("attachObject");
        SerializedProperty attachTargetProp = stepProp.FindPropertyRelative("attachTarget");
        SerializedProperty keepWorldTransformProp = stepProp.FindPropertyRelative("keepWorldTransform");
        SerializedProperty localPositionOffsetProp = stepProp.FindPropertyRelative("localPositionOffset");
        SerializedProperty localEulerOffsetProp = stepProp.FindPropertyRelative("localEulerOffset");

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Object", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(attachModeProp, new GUIContent("Attach Mode", "Target'a mi baglayacak, yoksa dunyaya mi birakacak?"));
            EditorGUILayout.PropertyField(attachObjectProp, new GUIContent("Attach Object", "Parent'i degisecek obje. Ornek: valiz."));

            if (attachObjectProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Attach Object bos. Bu step su an hicbir sey yapmayacak.", MessageType.Warning);
            }
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);

            AttachMode attachMode = (AttachMode)attachModeProp.enumValueIndex;
            if (attachMode == AttachMode.AttachToTarget)
            {
                EditorGUILayout.PropertyField(attachTargetProp, new GUIContent("Attach Target", "Objenin baglanacagi hedef transform. Ornek: hand_socket veya trunk_anchor."));

                if (attachTargetProp.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("AttachToTarget secili ama target bos. Bu step bir sey yapmayacak.", MessageType.Warning);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("DetachToWorld secili. Bu step objeyi parent'tan cikarir ve dunyada birakir.", MessageType.None);
            }
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Pose", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(keepWorldTransformProp, new GUIContent("Keep World Transform", "True ise parent degisir ama dunyadaki pozunu korur. False ise target'a snap olur."));

            if (!keepWorldTransformProp.boolValue && (AttachMode)attachModeProp.enumValueIndex == AttachMode.AttachToTarget)
            {
                EditorGUILayout.PropertyField(localPositionOffsetProp, new GUIContent("Local Position Offset", "Attach sonrasi target local offset'i."));
                EditorGUILayout.PropertyField(localEulerOffsetProp, new GUIContent("Local Rotation Offset", "Attach sonrasi target local rotation offset'i."));
            }
            else
            {
                EditorGUILayout.HelpBox("Keep World Transform acik oldugu icin local offset alanlari kullanilmayacak.", MessageType.None);
            }
        }
    }

    void DrawMentalModelBox()
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Mental Model", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "1) Move: yurur. Speed hareket hizidir.\n2) Move + Face Direction: once yone doner. Turn Speed sadece bu kismi etkiler.\n3) Rotate: oldugu yerde doner. Burada hiz yerine Duration vardir.\n4) Wait: sadece bekler.\n5) Animation: animator parametresi yollar.\n6) Fade: ekrani yumusakca karartir ya da acar.\n7) Teleport: objeyi world position'a aninda tasir.\n8) Attach: baska objeyi ele, bagaja ya da baska target'a parent eder.\n9) Event: hareket etmeden ekstra aksiyon baslatir.",
                MessageType.None);
        }
    }

    void AddStep(StepType type)
    {
        Undo.RecordObject(target, $"Add {type} Step");

        int newIndex = stepsProp.arraySize;
        stepsProp.arraySize++;
        EnsureFoldoutCount();

        SerializedProperty stepProp = stepsProp.GetArrayElementAtIndex(newIndex);
        ResetStep(stepProp, type);
        SetAllFoldouts(false);
        stepFoldouts[newIndex] = true;
        extraFoldouts[newIndex] = false;

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

    void DuplicateStep(int index)
    {
        Undo.RecordObject(target, "Duplicate Cutscene Step");

        bool foldoutState = index >= 0 && index < stepFoldouts.Count
            ? stepFoldouts[index]
            : true;
        bool extraFoldoutState = index >= 0 && index < extraFoldouts.Count
            ? extraFoldouts[index]
            : false;

        stepsProp.InsertArrayElementAtIndex(index);
        stepFoldouts.Insert(index, foldoutState);
        extraFoldouts.Insert(index, extraFoldoutState);

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

    void MoveStep(int from, int to)
    {
        Undo.RecordObject(target, "Move Cutscene Step");

        stepsProp.MoveArrayElement(from, to);

        if (from >= 0 && from < stepFoldouts.Count && to >= 0 && to < stepFoldouts.Count)
        {
            bool movedFoldout = stepFoldouts[from];
            stepFoldouts.RemoveAt(from);
            stepFoldouts.Insert(to, movedFoldout);
        }

        if (from >= 0 && from < extraFoldouts.Count && to >= 0 && to < extraFoldouts.Count)
        {
            bool movedExtraFoldout = extraFoldouts[from];
            extraFoldouts.RemoveAt(from);
            extraFoldouts.Insert(to, movedExtraFoldout);
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

    void DeleteStep(int index)
    {
        Undo.RecordObject(target, "Delete Cutscene Step");

        stepsProp.DeleteArrayElementAtIndex(index);

        if (index >= 0 && index < stepFoldouts.Count)
        {
            stepFoldouts.RemoveAt(index);
        }

        if (index >= 0 && index < extraFoldouts.Count)
        {
            extraFoldouts.RemoveAt(index);
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

    void ResetStep(SerializedProperty stepProp, StepType type)
    {
        stepProp.FindPropertyRelative("type").enumValueIndex = (int)type;
        stepProp.FindPropertyRelative("skip").boolValue = false;
        stepProp.FindPropertyRelative("waitForCompletion").boolValue = true;
        stepProp.FindPropertyRelative("startDelay").floatValue = 0f;
        stepProp.FindPropertyRelative("title").stringValue = string.Empty;
        stepProp.FindPropertyRelative("note").stringValue = string.Empty;

        stepProp.FindPropertyRelative("playAnimAtStart").boolValue = false;
        stepProp.FindPropertyRelative("animSetType").enumValueIndex = (int)AnimSetType.Trigger;
        stepProp.FindPropertyRelative("animParam").stringValue = string.Empty;
        stepProp.FindPropertyRelative("boolValue").boolValue = false;
        stepProp.FindPropertyRelative("floatValue").floatValue = 0f;
        stepProp.FindPropertyRelative("intValue").intValue = 0;
        stepProp.FindPropertyRelative("waitSeconds").floatValue = type == StepType.Wait ? 1f : 0f;

        stepProp.FindPropertyRelative("moveMode").enumValueIndex = (int)MoveMode.ByDistance;
        stepProp.FindPropertyRelative("direction").enumValueIndex = (int)Direction8.Forward;
        stepProp.FindPropertyRelative("customDirection").vector3Value = Vector3.forward;
        stepProp.FindPropertyRelative("distance").floatValue = 2f;
        stepProp.FindPropertyRelative("duration").floatValue = 1f;
        stepProp.FindPropertyRelative("speed").floatValue = 1.5f;
        stepProp.FindPropertyRelative("easing").enumValueIndex = (int)Easing.Linear;
        stepProp.FindPropertyRelative("faceDirection").boolValue = true;
        stepProp.FindPropertyRelative("turnSpeed").floatValue = 8f;

        stepProp.FindPropertyRelative("rotateMode").enumValueIndex = (int)RotateMode.WorldEuler;
        stepProp.FindPropertyRelative("worldEuler").vector3Value = Vector3.zero;
        stepProp.FindPropertyRelative("lookTarget").objectReferenceValue = null;
        stepProp.FindPropertyRelative("fadeMode").enumValueIndex = (int)FadeMode.FadeOutToBlack;
        stepProp.FindPropertyRelative("teleportWorldPosition").vector3Value = Vector3.zero;

        stepProp.FindPropertyRelative("attachMode").enumValueIndex = (int)AttachMode.AttachToTarget;
        stepProp.FindPropertyRelative("attachObject").objectReferenceValue = null;
        stepProp.FindPropertyRelative("attachTarget").objectReferenceValue = null;
        stepProp.FindPropertyRelative("keepWorldTransform").boolValue = false;
        stepProp.FindPropertyRelative("localPositionOffset").vector3Value = Vector3.zero;
        stepProp.FindPropertyRelative("localEulerOffset").vector3Value = Vector3.zero;

        ClearUnityEvent(stepProp.FindPropertyRelative("onInvoke"));
    }

    void ClearUnityEvent(SerializedProperty eventProp)
    {
        if (eventProp == null) return;

        SerializedProperty persistentCallsProp = eventProp.FindPropertyRelative("m_PersistentCalls");
        SerializedProperty callsProp = persistentCallsProp != null
            ? persistentCallsProp.FindPropertyRelative("m_Calls")
            : null;

        if (callsProp != null)
        {
            callsProp.ClearArray();
        }
    }

    void SetAllFoldouts(bool value)
    {
        EnsureFoldoutCount();
        for (int i = 0; i < stepFoldouts.Count; i++)
        {
            stepFoldouts[i] = value;
        }
    }

    void EnsureFoldoutCount()
    {
        if (stepsProp == null) return;

        while (stepFoldouts.Count < stepsProp.arraySize)
        {
            stepFoldouts.Add(false);
        }

        while (extraFoldouts.Count < stepsProp.arraySize)
        {
            extraFoldouts.Add(false);
        }

        while (stepFoldouts.Count > stepsProp.arraySize)
        {
            stepFoldouts.RemoveAt(stepFoldouts.Count - 1);
        }

        while (extraFoldouts.Count > stepsProp.arraySize)
        {
            extraFoldouts.RemoveAt(extraFoldouts.Count - 1);
        }

        if (stepsProp.arraySize > 0 && !stepFoldouts.Contains(true))
        {
            stepFoldouts[0] = true;
        }
    }

    void EnsureStyles()
    {
        if (cardTitleStyle == null)
        {
            cardTitleStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold
            };
        }

        if (cardSummaryStyle == null)
        {
            cardSummaryStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = true
            };
        }
    }

    string BuildStepHeader(SerializedProperty stepProp, int index)
    {
        string title = stepProp.FindPropertyRelative("title").stringValue;
        string autoSummary = BuildStepSummary(stepProp);

        string header = string.IsNullOrWhiteSpace(title)
            ? $"Step {index + 1}"
            : $"Step {index + 1} - {title}";

        if (!string.IsNullOrWhiteSpace(title))
        {
            header += $" ({autoSummary})";
        }

        if (stepProp.FindPropertyRelative("skip").boolValue)
        {
            header += " [SKIP]";
        }

        if (!stepProp.FindPropertyRelative("waitForCompletion").boolValue)
        {
            header += " [ASYNC]";
        }

        float startDelay = stepProp.FindPropertyRelative("startDelay").floatValue;
        if (startDelay > 0f)
        {
            header += $" [+{startDelay:0.##}s]";
        }

        return header;
    }

    string BuildStepSummary(SerializedProperty stepProp)
    {
        StepType type = (StepType)stepProp.FindPropertyRelative("type").enumValueIndex;

        switch (type)
        {
            case StepType.Move:
            {
                MoveMode moveMode = (MoveMode)stepProp.FindPropertyRelative("moveMode").enumValueIndex;
                Direction8 direction = (Direction8)stepProp.FindPropertyRelative("direction").enumValueIndex;
                float speed = stepProp.FindPropertyRelative("speed").floatValue;

                if (moveMode == MoveMode.ByDistance)
                {
                    float distance = stepProp.FindPropertyRelative("distance").floatValue;
                    return $"Move {Nicify(direction)} | {distance:0.##}m | {speed:0.##} m/s";
                }

                float duration = stepProp.FindPropertyRelative("duration").floatValue;
                return $"Move {Nicify(direction)} | {duration:0.##}s | {speed:0.##} m/s";
            }

            case StepType.Rotate:
            {
                RotateMode rotateMode = (RotateMode)stepProp.FindPropertyRelative("rotateMode").enumValueIndex;
                float duration = stepProp.FindPropertyRelative("duration").floatValue;

                if (rotateMode == RotateMode.LookAtTarget)
                {
                    Object targetObject = stepProp.FindPropertyRelative("lookTarget").objectReferenceValue;
                    return $"Rotate LookAt {SafeLabel(targetObject ? targetObject.name : string.Empty, "target")} | {duration:0.##}s";
                }

                Vector3 euler = stepProp.FindPropertyRelative("worldEuler").vector3Value;
                return $"Rotate World {euler.x:0.#}, {euler.y:0.#}, {euler.z:0.#} | {duration:0.##}s";
            }

            case StepType.Wait:
                return $"Wait {stepProp.FindPropertyRelative("waitSeconds").floatValue:0.##}s";

            case StepType.PlayAnimation:
            {
                AnimSetType animSetType = (AnimSetType)stepProp.FindPropertyRelative("animSetType").enumValueIndex;
                string param = stepProp.FindPropertyRelative("animParam").stringValue;
                return $"Anim {animSetType} {SafeLabel(param, "param")}";
            }

            case StepType.Fade:
            {
                FadeMode fadeMode = (FadeMode)stepProp.FindPropertyRelative("fadeMode").enumValueIndex;
                float duration = stepProp.FindPropertyRelative("duration").floatValue;
                string label = fadeMode == FadeMode.FadeOutToBlack ? "Fade Out" : "Fade In";
                return $"{label} | {duration:0.##}s";
            }

            case StepType.Teleport:
            {
                Vector3 pos = stepProp.FindPropertyRelative("teleportWorldPosition").vector3Value;
                return $"Teleport {pos.x:0.#}, {pos.y:0.#}, {pos.z:0.#}";
            }

            case StepType.Attach:
            {
                AttachMode attachMode = (AttachMode)stepProp.FindPropertyRelative("attachMode").enumValueIndex;
                Object attachObject = stepProp.FindPropertyRelative("attachObject").objectReferenceValue;
                Object attachTarget = stepProp.FindPropertyRelative("attachTarget").objectReferenceValue;

                if (attachMode == AttachMode.DetachToWorld)
                {
                    return $"Detach {SafeLabel(attachObject ? attachObject.name : string.Empty, "object")}";
                }

                return $"Attach {SafeLabel(attachObject ? attachObject.name : string.Empty, "object")} -> {SafeLabel(attachTarget ? attachTarget.name : string.Empty, "target")}";
            }

            case StepType.InvokeEvent:
                return "Invoke Event";
        }

        return "Step";
    }

    static void DrawMovePreview(CutsceneMover mover, Step step, int index, ref Vector3 currentPos, ref Quaternion currentRotation)
    {
        Vector3 worldDir = GetSceneDirection(mover, step, currentRotation);
        if (worldDir.sqrMagnitude < 0.0001f)
        {
            Handles.color = new Color(1f, 0.5f, 0.3f, 1f);
            Handles.Label(currentPos + Vector3.up * 0.45f, $"{index + 1}. Move (zero dir)");
            return;
        }

        worldDir.Normalize();

        if (!mover.forwardReference && step.faceDirection)
        {
            Vector3 flatDir = new Vector3(worldDir.x, 0f, worldDir.z);
            if (flatDir.sqrMagnitude > 0.0001f)
            {
                currentRotation = Quaternion.LookRotation(flatDir.normalized, Vector3.up);
            }
        }

        float distance = EstimateDistance(step);
        Vector3 nextPos = currentPos + worldDir * distance;

        Handles.color = new Color(0.25f, 0.85f, 1f, 1f);
        Handles.DrawAAPolyLine(4f, currentPos, nextPos);
        Handles.ArrowHandleCap(0, nextPos, Quaternion.LookRotation(worldDir), 0.7f, EventType.Repaint);

        string distanceText = step.moveMode == MoveMode.ByDuration ? $"~{distance:0.##}m" : $"{distance:0.##}m";
        Handles.Label(Vector3.Lerp(currentPos, nextPos, 0.5f) + Vector3.up * 0.25f, $"{index + 1}. Move {distanceText}");

        currentPos = nextPos;
    }

    static void DrawRotatePreview(Step step, int index, Vector3 currentPos, ref Quaternion currentRotation)
    {
        Handles.color = new Color(1f, 0.75f, 0.2f, 1f);

        if (step.rotateMode == RotateMode.LookAtTarget && step.lookTarget)
        {
            Handles.DrawDottedLine(currentPos, step.lookTarget.position, 4f);

            Vector3 flatDir = step.lookTarget.position - currentPos;
            flatDir.y = 0f;
            if (flatDir.sqrMagnitude > 0.0001f)
            {
                currentRotation = Quaternion.LookRotation(flatDir.normalized, Vector3.up);
            }

            Handles.Label(currentPos + Vector3.up * 0.55f, $"{index + 1}. Look At {step.lookTarget.name}");
            return;
        }

        currentRotation = Quaternion.Euler(step.worldEuler);
        Handles.ArrowHandleCap(0, currentPos, currentRotation, 0.7f, EventType.Repaint);
        Handles.Label(currentPos + Vector3.up * 0.55f, $"{index + 1}. Rotate {step.duration:0.##}s");
    }

    static void DrawTeleportPreview(Step step, int index, ref Vector3 currentPos)
    {
        Handles.color = new Color(0.9f, 0.2f, 0.9f, 1f);

        Vector3 nextPos = step.teleportWorldPosition;
        Handles.DrawDottedLine(currentPos, nextPos, 4f);
        Handles.SphereHandleCap(0, nextPos, Quaternion.identity, 0.18f, EventType.Repaint);
        Handles.Label(nextPos + Vector3.up * 0.25f, $"{index + 1}. Teleport");

        currentPos = nextPos;
    }

    static void DrawAttachPreview(Step step, int index, Vector3 currentPos)
    {
        Handles.color = new Color(0.95f, 0.5f, 1f, 1f);

        if (step.attachMode == AttachMode.DetachToWorld)
        {
            string objectName = step.attachObject ? step.attachObject.name : "object";
            Handles.Label(currentPos + Vector3.up * 0.55f, $"{index + 1}. Detach {objectName}");
            return;
        }

        if (step.attachObject && step.attachTarget)
        {
            Handles.DrawDottedLine(step.attachObject.position, step.attachTarget.position, 3f);
            Handles.Label(step.attachTarget.position + Vector3.up * 0.2f, $"{index + 1}. Attach {step.attachObject.name}");
            return;
        }

        Handles.Label(currentPos + Vector3.up * 0.55f, $"{index + 1}. Attach");
    }

    static Vector3 GetSceneDirection(CutsceneMover mover, Step step, Quaternion currentRotation)
    {
        Vector3 localDirection = GetDirectionVector(step.direction, step.customDirection);
        if (localDirection.sqrMagnitude < 0.0001f) return Vector3.zero;

        Quaternion referenceRotation = mover.forwardReference
            ? mover.forwardReference.rotation
            : currentRotation;

        return referenceRotation * localDirection.normalized;
    }

    static float EstimateDistance(Step step)
    {
        if (step.moveMode == MoveMode.ByDistance)
        {
            return Mathf.Max(0f, step.distance);
        }

        return Mathf.Max(0f, step.speed) * Mathf.Max(0f, step.duration);
    }

    static Vector3 GetDirectionVector(Direction8 dir, Vector3 custom)
    {
        switch (dir)
        {
            case Direction8.Forward: return new Vector3(0f, 0f, 1f);
            case Direction8.Back: return new Vector3(0f, 0f, -1f);
            case Direction8.Left: return new Vector3(-1f, 0f, 0f);
            case Direction8.Right: return new Vector3(1f, 0f, 0f);
            case Direction8.ForwardLeft: return new Vector3(-1f, 0f, 1f);
            case Direction8.ForwardRight: return new Vector3(1f, 0f, 1f);
            case Direction8.BackLeft: return new Vector3(-1f, 0f, -1f);
            case Direction8.BackRight: return new Vector3(1f, 0f, -1f);
            case Direction8.Custom: return custom;
        }

        return Vector3.zero;
    }

    static string Nicify(object value)
    {
        return ObjectNames.NicifyVariableName(value.ToString());
    }

    static string SafeLabel(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
