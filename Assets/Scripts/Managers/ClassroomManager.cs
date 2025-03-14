using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;

public class ClassroomManager : NetworkBehaviour
{
    [SyncVar]
    public int currentStudentId = 1;
    [SyncVar]
    public int currentBreadboardId = 1;

    [Serializable]
    public struct StudentScore
    {
        public int studentId;
        public string scoreText;
    }

    // SyncList for student scores - automatically syncs across all clients
    public readonly SyncList<StudentScore> studentScores = new SyncList<StudentScore>();

    [SerializeField] private GameObject scoreDisplayPrefab;
    [SerializeField] private Transform scoreDisplayParent;

    private Dictionary<int, GameObject> scoreDisplays = new Dictionary<int, GameObject>();

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Register callback for score changes
        studentScores.Callback += OnStudentScoresChanged;

        // Create score displays for existing students
        RefreshAllScoreDisplays();
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        // Clean up
        studentScores.Callback -= OnStudentScoresChanged;
        ClearScoreDisplays();
    }

    private void OnStudentScoresChanged(SyncList<StudentScore>.Operation op, int index, StudentScore oldItem, StudentScore newItem)
    {
        // Refresh UI when scores change
        RefreshAllScoreDisplays();
    }

    private void RefreshAllScoreDisplays()
    {
        ClearScoreDisplays();

        for (int i = 0; i < studentScores.Count; i++)
        {
            StudentScore score = studentScores[i];
            CreateScoreDisplay(score);
        }
    }

    private void ClearScoreDisplays()
    {
        foreach (var display in scoreDisplays.Values)
        {
            Destroy(display);
        }
        scoreDisplays.Clear();
    }

    private void CreateScoreDisplay(StudentScore score)
    {
        if (scoreDisplayPrefab == null || scoreDisplayParent == null)
        {
            Debug.LogError("Score display prefab or parent not assigned!");
            return;
        }

        GameObject displayObj = Instantiate(scoreDisplayPrefab, scoreDisplayParent);
        scoreDisplays[score.studentId] = displayObj;

        TMP_Text textComponent = displayObj.GetComponent<TMP_Text>();
        if (textComponent != null)
        {
            textComponent.text = score.scoreText;
        }
    }

    // Server command to update a student score
    [Command(ignoreAuthority = true)]
    public void CmdUpdateStudentScore(int studentId, string scoreText)
    {
        // Find if this student already exists
        for (int i = 0; i < studentScores.Count; i++)
        {
            if (studentScores[i].studentId == studentId)
            {
                // Update existing student
                StudentScore updatedScore = studentScores[i];
                updatedScore.scoreText = scoreText;
                studentScores[i] = updatedScore;
                return;
            }
        }

        // Add new student
        StudentScore newScore = new StudentScore
        {
            studentId = studentId,
            scoreText = scoreText
        };
        studentScores.Add(newScore);
    }

    // Server command to remove a student
    [Command(ignoreAuthority = true)]
    public void CmdRemoveStudent(int studentId)
    {
        for (int i = 0; i < studentScores.Count; i++)
        {
            if (studentScores[i].studentId == studentId)
            {
                studentScores.RemoveAt(i);
                return;
            }
        }
    }

    [Command(ignoreAuthority = true)]
    public void CmdIncrementStudentId()
    {
        currentStudentId++;
    }

    [Command(ignoreAuthority = true)]
    public void CmdIncrementBreadboardId()
    {
        currentBreadboardId++;
    }


}
