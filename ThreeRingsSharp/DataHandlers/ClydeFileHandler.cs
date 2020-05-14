﻿using com.threerings.export;
using com.threerings.math;
using com.threerings.opengl.model.config;
using java.io;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ThreeRingsSharp.Utility;
using ThreeRingsSharp.Utility.Interface;
using ThreeRingsSharp.XansData;
using ThreeRingsSharp.XansData.Exceptions;

namespace ThreeRingsSharp.DataHandlers {

	/// <summary>
	/// A class designed to handle files exported by the Clyde library.
	/// </summary>
	public class ClydeFileHandler {

		/// <summary>
		/// A delegate action that is called when the GUI needs to update. This can safely be <see langword="null"/> for contexts that do not have a GUI, as it is used for the SK Animator Tools UI.<para/>
		/// Pass in <see langword="null"/> for arguments to make their data remain unchanged.
		/// </summary>
		public static Action<string, string, string, string> UpdateGUIAction { get; set; } = null;

		/// <summary>
		/// Takes in a <see cref="FileInfo"/> representing a file that was created with the Clyde library.<para/>
		/// This will throw a 
		/// </summary>
		/// <param name="clydeFile">The file to load and decode.</param>
		/// <param name="allGrabbedModels">A list containing every processed model from the entire hierarchy.</param>
		/// <param name="isBaseFile">If <see langword="true"/>, this will update the main GUI (if it is not null) display data for the base loaded model.</param>
		/// <param name="lastNodeParent">Intended for use if <paramref name="isBaseFile"/> is <see langword="false"/>, this is the parent Data Tree element to add this model into (so that the hierarchy can be constructed). This should be a <see cref="TreeNode"/>, a <see cref="TreeView"/>, or a <see cref="DataTreeObject"/>.</param>
		/// <param name="useFileName">If <see langword="true"/>, the name of the loaded file will be displayed, followed by its type in brackets: model.dat [ModelConfig]</param>
		/// <param name="transform">Intended to be used by reference loaders, this specifies an offset for referenced models. All models loaded by this method in the given chain / hierarchy will have this transform applied to them.</param>
		public static void HandleClydeFile(FileInfo clydeFile, List<Model3D> allGrabbedModels, bool isBaseFile = false, dynamic lastNodeParent = null, bool useFileName = false, Transform3D transform = null) {
			// TODO: Do NOT update the main GUI if this is a referenced file (e.g. a CompoundConfig wants to load other data, don't change the table view in the top right)
			XanLogger.WriteLine($"Loading [{clydeFile.FullName}]...");
			// ProgramLog.Update();
			if (!VersionInfoScraper.IsValidClydeFile(clydeFile)) {
				XanLogger.WriteLine("Invalid file. Sending error.");
				// if (isBaseFile) AsyncMessageBox.ShowAsync("This file isn't a valid Clyde file! (Reason: Incorrect header)", "Invalid File", MessageBoxButtons.OK, MessageBoxIcon.Error);
				throw new ClydeDataReadException("This file isn't a valid Clyde file! (Reason: Incorrect header)");
			}
			(string, string, string) cosmeticInfo = VersionInfoScraper.GetCosmeticInformation(clydeFile);
			XanLogger.WriteLine($"Read file to grab the raw info.");
			string modelFullClass = cosmeticInfo.Item3;
			string[] modelClassInfo = ClassNameStripper.GetSplitClassName(modelFullClass);

			string modelClass = null;
			string modelSubclass = null;
			if (modelClassInfo != null) {
				modelClass = modelClassInfo[0];
				if (modelClassInfo.Length == 2) modelSubclass = modelClassInfo[1];
			}

			if (modelClass == "ProjectXModelConfig") {
				XanLogger.WriteLine("User imported a Player Knight model. These are unsupported. Sending warning.");
				if (isBaseFile) AsyncMessageBox.Show("Player Knights do not use the standard ArticulatedConfig type (used for all animated character models) and instead use a unique type called ProjectXModelConfig. Unless the Spiral Knights jar is directly referenced, this type cannot be loaded.\n\nThankfully, an automatic fix will be employed for you! I'm going to load /rsrc/character/npc/crew/model.dat instead, which is a Knight model that uses ArticulatedConfig since it's an NPC.", "Knights are not supported!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				string characterFolder = clydeFile.Directory.Parent.FullName;
				if (!characterFolder.EndsWith("/")) characterFolder += "/";
				clydeFile = new FileInfo(characterFolder + "npc/crew/model.dat");
				if (!clydeFile.Exists) {
					XanLogger.WriteLine("Failed to locate substitute crew NPC model in target directory.");
					//if (isBaseFile) AsyncMessageBox.ShowAsync("Oh no! The file at " + clydeFile.FullName + " doesn't exist :(\nThis path is intended to work only for cases where you loaded /rsrc/character/pc/model.dat directly, so if this was a custom saved .DAT file, this error was bound to happen.", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					if (isBaseFile && UpdateGUIAction != null) {
						UpdateGUIAction("Nothing Loaded!", "N/A", "N/A", "N/A");
					}
					throw new ClydeDataReadException("The file at " + clydeFile.FullName + " doesn't exist!");
				}
			}


			if (isBaseFile && UpdateGUIAction != null) {
				UpdateGUIAction(clydeFile.Name, cosmeticInfo.Item1, cosmeticInfo.Item2, "Processing...");
			}

			DataInputStream dataInput = new DataInputStream(new FileInputStream(clydeFile.FullName));
			BinaryImporter importer = new BinaryImporter(dataInput);
			ModelConfigBrancher currentBrancher = new ModelConfigBrancher();


			var obj = (java.lang.Object)importer.readObject();
			DataTreeObject rootDataTreeObject = new DataTreeObject();

			if (obj is ModelConfig model) {
				if (isBaseFile && UpdateGUIAction != null) {
					UpdateGUIAction(null, null, null, "ModelConfig");
				}

				if (transform != null) transform.promote(Transform3D.GENERAL); // Highest level, 4x4 transformation matrix.
				currentBrancher.HandleDataFrom(clydeFile, model, allGrabbedModels, rootDataTreeObject, useFileName, transform);
			} else {
				if (isBaseFile && UpdateGUIAction != null) {
					UpdateGUIAction(null, null, null, "Unknown");
				}
				//if (isBaseFile) AsyncMessageBox.ShowAsync("Oh Fiddlesticks! While this *is* a valid .DAT file, I'm afraid I can't actually do anything with this data!\n\nData Class:\n" + obj.getClass().getTypeName(), "Invalid Type", MessageBoxButtons.OK, MessageBoxIcon.Error);
				rootDataTreeObject.Text = modelClass;
				rootDataTreeObject.ImageKey = SilkImage.Generic;
				throw new ClydeDataReadException("This .DAT file is valid, but its type is unknown (The system is only designed to handle ModelConfigs).");
			}

			if (lastNodeParent is TreeNode || lastNodeParent is TreeView) {
				lastNodeParent.Nodes.Add(rootDataTreeObject.ConvertHierarchyToTreeNodes());
			} else if (lastNodeParent is DataTreeObject datObj) {
				rootDataTreeObject.Parent = datObj;
			}
			return;
		}
	}
}