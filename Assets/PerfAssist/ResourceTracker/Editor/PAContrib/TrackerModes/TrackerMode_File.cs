﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using System;
using UnityEditor.MemoryProfiler;
using MemoryProfilerWindow;

public class TrackerMode_File : TrackerMode_Base
{
    public override void OnGUI()
    {
        if (_indices != null)
        {
            var newIndex = GUI.SelectionGrid(new Rect(210, 0, 30 * _indices.Length, 20), _selected, _indices, _indices.Length, MemStyles.ToolbarButton);
            if (newIndex != _selected)
            {
                _selected = newIndex;

                SelectionChanged();
            }
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Load Session", GUILayout.MaxWidth(100)))
        {
            LoadSession();

            SelectionChanged();
        }
    }

    public void LoadSession()
    {
        string pathName = EditorUtility.OpenFolderPanel("Load Snapshot Folder", MemUtil.SnapshotsDir, "");
        if (string.IsNullOrEmpty(pathName))
            return;

        List<object> packeds = new List<object>();
        try
        {
            DirectoryInfo TheFolder = new DirectoryInfo(pathName);
            if (!TheFolder.Exists)
                throw new Exception(string.Format("bad path: {0}", TheFolder.ToString()));

            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            foreach (var file in TheFolder.GetFiles())
            {
                var fileName = file.FullName;
                if (fileName.EndsWith(".memsnap"))
                {
                    using (Stream stream = File.Open(fileName, FileMode.Open))
                    {
                        packeds.Add(bf.Deserialize(stream));
                    }
                }
            }

            if (packeds.Count == 0)
            {
                MemUtil.NotifyError("no snapshots found.");
                return;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(string.Format("load snapshot error ! msg ={0}", ex.Message));
            return;
        }

        Clear();

        foreach (var obj in packeds)
        {
            MemSnapshotInfo memInfo = new MemSnapshotInfo();

            var packed = obj as PackedMemorySnapshot;
            MemUtil.LoadSnapshotProgress(0.01f, "creating Crawler");
            var packedCrawled = new Crawler().Crawl(packed);
            MemUtil.LoadSnapshotProgress(0.7f, "unpacking");
            memInfo.unPacked = CrawlDataUnpacker.Unpack(packedCrawled);
            MemUtil.LoadSnapshotProgress(1.0f, "done");

            _snapshots.Add(memInfo);
        }

        if (_snapshots.Count == 0)
        {
            MemUtil.NotifyError("empty snapshot list, ignored.");
            return;
        }

        RefreshIndices();
        _selected = _snapshots.Count - 1;
    }
}
