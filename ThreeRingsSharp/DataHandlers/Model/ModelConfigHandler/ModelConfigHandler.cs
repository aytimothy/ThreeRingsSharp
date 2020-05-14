﻿using com.threerings.opengl.model.config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreeRingsSharp.Utility.Interface;
using static com.threerings.opengl.model.config.ModelConfig;
using static com.threerings.opengl.model.config.ModelConfig.Imported;
using static com.threerings.opengl.model.config.ModelConfig.Implementation;
using ThreeRingsSharp.Utility;

namespace ThreeRingsSharp.DataHandlers.Model.ModelConfigHandlers {
	public class ModelConfigHandler {

		/// <summary>
		/// Sets up the cosmetic data for this model, or, what's displayed in the GUI for the program.<para/>
		/// This specific method populates data that is common across all imported models.
		/// </summary>
		/// <param name="model">The model containing other data, such as the <see cref="Implementation"/>.</param>
		/// <param name="dataTreeParent">This is the instance in the data tree that represents this object in the hierarchy. If null, this method call is skipped.</param>
		/// <param name="dataTreeNameIsImplementation">If <see langword="true"/>, the name of <paramref name="dataTreeParent"/> is the implementation of the model, so the implementation property will not be added.</param>
		public static void SetupCosmeticInformation(ModelConfig model, DataTreeObject dataTreeParent, bool dataTreeNameIsImplementation) {
			if (dataTreeParent == null) return;
			Implementation imp = model.implementation;

			if (!dataTreeNameIsImplementation) {
				// If this is true, the name of the actual node is the implementation, rendering this property useless.

				string implementationName = ClassNameStripper.GetWholeClassName(imp.getClass().getName()) ?? "Unknown Implementation";
				dataTreeParent.AddSimpleProperty("Implementation", implementationName.Replace("$", "::"), SilkImage.Config, SilkImage.Config, true);
			}

			// It's imported!
			// ...Unless it's a CompoundConfig. (I mean given the properties below it makes sense, it's just a container, not actual model data.)
			if (imp is Imported) {
				Imported imported = (Imported)imp;

				//RootDataTreeObject.AddSimpleProperty("Scale", model.scale);
				List<object> influences = new List<object>(3);
				if (imported.influences.fog) influences.Add(new DataTreeObjectProperty("Fog", SilkImage.Shading));
				if (imported.influences.lights) influences.Add(new DataTreeObjectProperty("Lights", SilkImage.Light));
				if (imported.influences.projections) influences.Add(new DataTreeObjectProperty("Projections", SilkImage.Texture));
				if (influences.Count > 0) dataTreeParent.AddSimpleProperty("Influenced By...", influences.ToArray(), displaySinglePropertiesInline: false);

				MaterialMapping[] matMaps = imported.materialMappings;
				object[] materials = new object[matMaps.Length];
				for (int idx = 0; idx < materials.Length; idx++) {
					materials[idx] = matMaps[idx].texture;
				}
				dataTreeParent.AddSimpleProperty("Textures", materials, SilkImage.Value, SilkImage.Texture, false);
			}
		}

	}
}