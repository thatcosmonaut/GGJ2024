using ImGuiNET;
using MoonWorks.Graphics;
using MoonWorks.Input;
using MoonWorks;
using System.Numerics;
using System.IO;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ContentBuilderUI
{
	class ContentBuilderUIGame : Game
	{
		private string ShaderContentPath = "Content/Shaders";
		private string FontContentPath = Path.Combine("Content", "Fonts");

		private DebugTextureStorage TextureStorage;
		private Texture FontTexture;

		private uint VertexCount = 0;
		private uint IndexCount = 0;
		private MoonWorks.Graphics.Buffer ImGuiVertexBuffer = null;
		private MoonWorks.Graphics.Buffer ImGuiIndexBuffer = null;
		private GraphicsPipeline ImGuiPipeline;
		private Shader ImGuiVertexShader;
		private Shader ImGuiFragmentShader;
		private Sampler ImGuiSampler;

		private ResourceUploader BufferUploader;

		private string unprocessedContentPath = "";
		private string projectPath = "";

		private bool ContentPathValid = false;
		private bool ProjectPathValid = false;

		public unsafe ContentBuilderUIGame(
			AppInfo appInfo,
			WindowCreateInfo windowCreateInfo,
			FramePacingSettings frameLimiterSettings,
			bool debugMode
		) : base(appInfo, windowCreateInfo, frameLimiterSettings, ShaderFormat.SPIRV, debugMode)
		{
			Operations.Initialize();

			if (Operations.Preferences != null)
			{
				if (Operations.Preferences.SourceContentDirectoryPath != null)
				{
					unprocessedContentPath = Operations.Preferences.SourceContentDirectoryPath;
				}
				if (Operations.Preferences.GameDirectoryPath != null)
				{
					projectPath = Operations.Preferences.GameDirectoryPath;
				}

				ContentPathValid = Operations.ValidateSourceContentDirectory(unprocessedContentPath);
				ProjectPathValid = Operations.ValidateGameProjectDirectory(projectPath);
			}

			TextureStorage = new DebugTextureStorage();

			ImGui.CreateContext();

			var io = ImGui.GetIO();

			io.Fonts.AddFontFromFileTTF(
				Path.Combine(FontContentPath, "FiraCode-Regular.ttf"),
				16
			);

			ImFontConfigPtr fontConfig = ImGuiNative.ImFontConfig_ImFontConfig();
			fontConfig.MergeMode = true;

			var glyph_ranges = stackalloc ushort[]
			{
				0xE800, 0xE801, // check mark and X mark
				0xE832, 0xE832, // spinner
				0xF0EC, 0xF0EC, // compare
				0
			};

			io.Fonts.AddFontFromFileTTF(
				Path.Combine(FontContentPath, "fontello.ttf"),
				16,
				fontConfig,
				(nint)glyph_ranges
			);
			io.Fonts.Build();

			Inputs.TextInput += c =>
			{
				if (c == '\t') { return; }
				io.AddInputCharacter(c);
			};

			io.DisplaySize = new System.Numerics.Vector2(MainWindow.Width, MainWindow.Height);
			io.DisplayFramebufferScale = System.Numerics.Vector2.One;

			ImGuiVertexShader = Shader.Create(
				GraphicsDevice,
				RootTitleStorage,
				$"{ShaderContentPath}/ImGui.vert.spv",
				"main",
				new ShaderCreateInfo
				{
					Format = ShaderFormat.SPIRV,
					Stage = ShaderStage.Vertex,
					NumUniformBuffers = 1
				}
			);

			ImGuiFragmentShader = Shader.Create(
				GraphicsDevice,
				RootTitleStorage,
				$"{ShaderContentPath}/ImGui.frag.spv",
				"main",
				new ShaderCreateInfo
				{
					Format = ShaderFormat.SPIRV,
					Stage = ShaderStage.Fragment,
					NumSamplers = 1
				}
			);

			ImGuiSampler = Sampler.Create(GraphicsDevice, SamplerCreateInfo.LinearClamp);

			ImGuiPipeline = GraphicsPipeline.Create(
				GraphicsDevice,
				new GraphicsPipelineCreateInfo
				{
					TargetInfo = new GraphicsPipelineTargetInfo
					{
						ColorTargetDescriptions = [
							new ColorTargetDescription
							{
								Format = MainWindow.SwapchainFormat,
								BlendState = ColorTargetBlendState.NonPremultipliedAlphaBlend
							}
						]
					},
					DepthStencilState = DepthStencilState.Disable,
					VertexShader = ImGuiVertexShader,
					FragmentShader = ImGuiFragmentShader,
					VertexInputState = VertexInputState.CreateSingleBinding<Position2DTextureColorVertex>(),
					PrimitiveType = PrimitiveType.TriangleList,
					RasterizerState = RasterizerState.CW_CullNone,
					MultisampleState = MultisampleState.None
				}
			);

			BufferUploader = new ResourceUploader(GraphicsDevice);
			BuildFontAtlas();
		}

		protected override void Update(System.TimeSpan dt)
		{
			var io = ImGui.GetIO();

			if (io.WantCaptureKeyboard)
			{
				MainWindow.StartTextInput();
			}
			else if (!io.WantCaptureKeyboard)
			{
				MainWindow.StopTextInput();
			}

			io.AddMousePosEvent(Inputs.Mouse.X, Inputs.Mouse.Y);
			io.AddMouseButtonEvent(0, Inputs.Mouse.LeftButton.IsDown);
			io.AddMouseButtonEvent(1, Inputs.Mouse.RightButton.IsDown);
			io.AddMouseButtonEvent(2, Inputs.Mouse.MiddleButton.IsDown);
			io.AddMouseWheelEvent(0f, Inputs.Mouse.Wheel);

			// TODO: set up io.AddKeyEvents for keyboard keys

			io.KeyShift = Inputs.Keyboard.IsDown(KeyCode.LeftShift) || Inputs.Keyboard.IsDown(KeyCode.RightShift);
			io.KeyCtrl = Inputs.Keyboard.IsDown(KeyCode.LeftControl) || Inputs.Keyboard.IsDown(KeyCode.RightControl);
			io.KeyAlt = Inputs.Keyboard.IsDown(KeyCode.LeftAlt) || Inputs.Keyboard.IsDown(KeyCode.RightAlt);
			io.KeySuper = Inputs.Keyboard.IsDown(KeyCode.LeftMeta) || Inputs.Keyboard.IsDown(KeyCode.RightMeta);

			io.AddKeyEvent(ImGuiKey.A, Inputs.Keyboard.IsDown(KeyCode.A));
			io.AddKeyEvent(ImGuiKey.Z, Inputs.Keyboard.IsDown(KeyCode.Z));
			io.AddKeyEvent(ImGuiKey.Y, Inputs.Keyboard.IsDown(KeyCode.Y));
			io.AddKeyEvent(ImGuiKey.X, Inputs.Keyboard.IsDown(KeyCode.X));
			io.AddKeyEvent(ImGuiKey.C, Inputs.Keyboard.IsDown(KeyCode.C));
			io.AddKeyEvent(ImGuiKey.V, Inputs.Keyboard.IsDown(KeyCode.V));

			io.AddKeyEvent(ImGuiKey.Tab, Inputs.Keyboard.IsDown(KeyCode.Tab));
			io.AddKeyEvent(ImGuiKey.LeftArrow, Inputs.Keyboard.IsDown(KeyCode.Left));
			io.AddKeyEvent(ImGuiKey.RightArrow, Inputs.Keyboard.IsDown(KeyCode.Right));
			io.AddKeyEvent(ImGuiKey.UpArrow, Inputs.Keyboard.IsDown(KeyCode.Up));
			io.AddKeyEvent(ImGuiKey.DownArrow, Inputs.Keyboard.IsDown(KeyCode.Down));
			io.AddKeyEvent(ImGuiKey.Enter, Inputs.Keyboard.IsDown(KeyCode.Return));
			io.AddKeyEvent(ImGuiKey.Escape, Inputs.Keyboard.IsDown(KeyCode.Escape));
			io.AddKeyEvent(ImGuiKey.Delete, Inputs.Keyboard.IsDown(KeyCode.Delete));
			io.AddKeyEvent(ImGuiKey.Backspace, Inputs.Keyboard.IsDown(KeyCode.Backspace));
			io.AddKeyEvent(ImGuiKey.Home, Inputs.Keyboard.IsDown(KeyCode.Home));
			io.AddKeyEvent(ImGuiKey.End, Inputs.Keyboard.IsDown(KeyCode.End));
			io.AddKeyEvent(ImGuiKey.PageDown, Inputs.Keyboard.IsDown(KeyCode.PageDown));
			io.AddKeyEvent(ImGuiKey.PageUp, Inputs.Keyboard.IsDown(KeyCode.PageUp));

			if (Inputs.Keyboard.IsDown(KeyCode.LeftControl) && Inputs.Keyboard.IsPressed(KeyCode.V))
			{
				var pasteString = SDL3.SDL.SDL_GetClipboardText();
				io.AddInputCharactersUTF8(pasteString);
			}

			// Style
			var hover = UIColors.RGB255(73, 46, 46);
			ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 1);
			ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 3);
			ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(15, 15));
			ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new System.Numerics.Vector2(5, 1));
			ImGui.PushStyleColor(ImGuiCol.FrameBg, UIColors.Transparent);
			ImGui.PushStyleColor(ImGuiCol.FrameBgActive, hover);
			ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, hover);
			ImGui.PushStyleColor(ImGuiCol.Border, UIColors.RedText);
			ImGui.PushStyleColor(ImGuiCol.WindowBg, UIColors.Background);
			ImGui.PushStyleColor(ImGuiCol.TextDisabled, UIColors.Disabled);
			ImGui.PushStyleColor(ImGuiCol.TextSelectedBg, hover);
			ImGui.PushStyleColor(ImGuiCol.Text, UIColors.Text);
			ImGui.PushStyleColor(ImGuiCol.Button, UIColors.Transparent);
			ImGui.PushStyleColor(ImGuiCol.ButtonHovered, hover);
			ImGui.PushStyleColor(ImGuiCol.ButtonActive, UIColors.SamuraiGunn2Red);
			ImGui.PushStyleColor(ImGuiCol.Separator, hover);
			ImGui.PushStyleColor(ImGuiCol.SeparatorHovered, hover);
			ImGui.PushStyleColor(ImGuiCol.PopupBg, UIColors.InkBlack);

			ImGui.NewFrame();

			ImGui.SetNextWindowSize(io.DisplaySize);
			ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, 0));
			ImGui.Begin("Main", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove);

			#region Content Path
			ImGui.PushStyleColor(ImGuiCol.Text, BoolToColor(ContentPathValid));
			ImGui.PushStyleColor(ImGuiCol.Border, BoolToColor(ContentPathValid));
			ImGui.Text(BoolToEmoji(ContentPathValid));
			ImGui.SameLine();
			if (ImGui.InputText("Unprocessed Content Path", ref unprocessedContentPath, 255))
			{
				ContentPathValid = Operations.ValidateSourceContentDirectory(unprocessedContentPath);
			}
			ImGui.PopStyleColor(2);
			#endregion

			#region Project Path
			ImGui.PushStyleColor(ImGuiCol.Text, BoolToColor(ProjectPathValid));
			ImGui.PushStyleColor(ImGuiCol.Border, BoolToColor(ProjectPathValid));
			ImGui.Text(BoolToEmoji(ProjectPathValid));
			ImGui.SameLine();
			if (ImGui.InputText("Project Path", ref projectPath, 255))
			{
				ProjectPathValid = Operations.ValidateGameProjectDirectory(projectPath);
			}
			ImGui.PopStyleColor(2);
			#endregion

			ImGui.Spacing();
			ImGui.Spacing();

			#region Buttons
			ImGui.Columns(1, "Buttons", true);
			if (ContentPathValid && ProjectPathValid)
			{
				if (ImGui.Button("Check Content Directories"))
				{
					foreach (var trackedDirectory in Operations.AllTrackedDirectories)
					{
						Task.Run(() =>
						{
							trackedDirectory.LoadHashFromDisk();
							trackedDirectory.CalculateContentHash();
							trackedDirectory.UpdateBuildStatus();
						});
					}
				}

				ImGui.SameLine();
				if (ImGui.Button("Build Content"))
				{
					Operations.BuildOutOfDate();
				}
			}
			else
			{
				ImGui.Text("Enter Content and Project Paths to continue");
			}

			ImGui.Spacing();
			ImGui.Spacing();
			#endregion

			ImGui.Columns(2);
			ImGui.SetColumnWidth(0, 350);
			ImGui.SetColumnWidth(1, 30);
			if (ContentPathValid && ProjectPathValid)
			{
				DrawContentGroup(Operations.Sprites);
				ImGui.Separator();
				DrawContentGroup(Operations.Audio);
				ImGui.Separator();
				DrawContentGroup(Operations.Fonts);
				ImGui.Separator();

				foreach (var trackedDirectory in Operations.Other)
				{
					DrawTrackedDirectory(trackedDirectory);
				}
			}

			ImGui.PopStyleVar(5);
			ImGui.PopStyleColor(14);
			ImGui.End();
			ImGui.EndFrame();
		}

		public void DrawContentGroup(ContentGroup contentGroup)
		{
			if (ImGui.TreeNodeEx(contentGroup.Name))
			{
				ImGui.NextColumn();
				DrawBuildStatus(contentGroup.BuildStatus);
				ImGui.NextColumn();

				foreach (var trackedDirectory in contentGroup)
				{
					DrawTrackedDirectory(trackedDirectory);
				}

				ImGui.TreePop();
			}
			else
			{
				ImGui.NextColumn();
				DrawBuildStatus(contentGroup.BuildStatus);
				ImGui.NextColumn();
			}
		}

		private void DrawTrackedDirectory(TrackedDirectory trackedDirectory)
		{
			var name = Path.GetFileName(trackedDirectory.DirectoryPath);
			if (trackedDirectory.BuildStatus != BuildStatus.InProgress)
			{
				if (ImGui.Button(name))
				{
					Task.Run(() => Operations.ProcessTrackedDir(trackedDirectory));
				}
				if (ImGui.IsItemHovered())
				{
					ImGui.SetTooltip("Build " + name);
				}
			}
			else
			{
				ImGui.Text(name);
			}

			ImGui.NextColumn();
			DrawBuildStatus(trackedDirectory.BuildStatus);
			ImGui.NextColumn();
		}

		private void DrawBuildStatus(BuildStatus buildStatus)
		{
			ImGui.PushStyleColor(ImGuiCol.Text, BuildStatusToColor(buildStatus));
			ImGui.Text(BuildStatusToEmoji(buildStatus));
			ImGui.PopStyleColor();
		}

		private System.Numerics.Vector4 BoolToColor(bool b)
		{
			if (b)
				return UIColors.Positive;
			else return UIColors.Negative;
		}

		private string BoolToEmoji(bool input)
		{
			return input ? "\uE800" : "\uE801";
		}

		private string BuildStatusToEmoji(BuildStatus buildStatus)
		{
			return buildStatus switch
			{
				BuildStatus.OutOfDate => "\uE801",
				BuildStatus.InProgress => "\uE832",
				BuildStatus.Comparing => "\uF0EC",
				_ => "\uE800"
			};
		}

		private System.Numerics.Vector4 BuildStatusToColor(BuildStatus buildStatus)
		{
			return buildStatus switch
			{
				BuildStatus.OutOfDate => UIColors.Negative,
				BuildStatus.InProgress => UIColors.Progress,
				BuildStatus.Comparing => UIColors.Progress,
				_ => UIColors.Positive
			};
		}

		protected override void Draw(double alpha)
		{
			ImGui.Render();

			var io = ImGui.GetIO();
			var drawDataPtr = ImGui.GetDrawData();

			UpdateImGuiBuffers(drawDataPtr);

			var commandBuffer = GraphicsDevice.AcquireCommandBuffer();
			var swapchainTexture = commandBuffer.AcquireSwapchainTexture(MainWindow);

			RenderCommandLists(commandBuffer, swapchainTexture, drawDataPtr, io);

			GraphicsDevice.Submit(commandBuffer);
		}

		protected override void Destroy()
		{

		}

		private unsafe void BuildFontAtlas()
		{
			var textureUploader = new ResourceUploader(GraphicsDevice);

			var io = ImGui.GetIO();

			io.Fonts.GetTexDataAsRGBA32(
				out System.IntPtr pixelData,
				out int width,
				out int height,
				out int bytesPerPixel
			);

			var pixelSpan = new ReadOnlySpan<Color>((void*)pixelData, width * height);

			FontTexture = textureUploader.CreateTexture2D(
				pixelSpan,
				TextureFormat.R8G8B8A8Unorm,
				TextureUsageFlags.Sampler,
                (uint)width,
                (uint)height);

			textureUploader.Upload();
			textureUploader.Dispose();

			io.Fonts.SetTexID(FontTexture.Handle);
			io.Fonts.ClearTexData();

			TextureStorage.Add(FontTexture);
		}

		private unsafe void UpdateImGuiBuffers(ImDrawDataPtr drawDataPtr)
		{
			if (drawDataPtr.TotalVtxCount == 0) { return; }

			var commandBuffer = GraphicsDevice.AcquireCommandBuffer();

			if (drawDataPtr.TotalVtxCount > VertexCount)
			{
				ImGuiVertexBuffer?.Dispose();

				VertexCount = (uint)(drawDataPtr.TotalVtxCount * 1.5f);
				ImGuiVertexBuffer = MoonWorks.Graphics.Buffer.Create<Position2DTextureColorVertex>(
					GraphicsDevice,
					BufferUsageFlags.Vertex,
					VertexCount
				);
			}

			if (drawDataPtr.TotalIdxCount > IndexCount)
			{
				ImGuiIndexBuffer?.Dispose();

				IndexCount = (uint)(drawDataPtr.TotalIdxCount * 1.5f);
				ImGuiIndexBuffer = MoonWorks.Graphics.Buffer.Create<ushort>(
					GraphicsDevice,
					BufferUsageFlags.Index,
					IndexCount
				);
			}

			uint vertexOffset = 0;
			uint indexOffset = 0;

			for (var n = 0; n < drawDataPtr.CmdListsCount; n += 1)
			{
				var cmdList = drawDataPtr.CmdLists[n];

				BufferUploader.SetBufferData(
					ImGuiVertexBuffer,
					vertexOffset,
					new ReadOnlySpan<Position2DTextureColorVertex>((void*) cmdList.VtxBuffer.Data, cmdList.VtxBuffer.Size),
					n == 0
				);

				BufferUploader.SetBufferData(
					ImGuiIndexBuffer,
					indexOffset,
					new ReadOnlySpan<ushort>((void*) cmdList.IdxBuffer.Data, cmdList.IdxBuffer.Size),
					n == 0
				);

				vertexOffset += (uint)cmdList.VtxBuffer.Size;
				indexOffset += (uint)cmdList.IdxBuffer.Size;
			}

			BufferUploader.Upload();
			GraphicsDevice.Submit(commandBuffer);
		}

		private void RenderCommandLists(CommandBuffer commandBuffer, Texture renderTexture, ImDrawDataPtr drawDataPtr, ImGuiIOPtr ioPtr)
		{
			var view = Matrix4x4.CreateLookAt(
				new Vector3(0, 0, 1),
				Vector3.Zero,
				Vector3.UnitY
			);

			var projection = Matrix4x4.CreateOrthographicOffCenter(
				0,
				480,
				270,
				0,
				0.01f,
				4000f
			);

			var viewProjectionMatrix = view * projection;

			var renderPass = commandBuffer.BeginRenderPass(
				new ColorTargetInfo(renderTexture, Color.White)
			);

			renderPass.BindGraphicsPipeline(ImGuiPipeline);

			commandBuffer.PushVertexUniformData(
				Matrix4x4.CreateOrthographicOffCenter(0, ioPtr.DisplaySize.X, ioPtr.DisplaySize.Y, 0, -1, 1)
			);

			renderPass.BindVertexBuffers(ImGuiVertexBuffer);
			renderPass.BindIndexBuffer(ImGuiIndexBuffer, IndexElementSize.Sixteen);

			uint vertexOffset = 0;
			uint indexOffset = 0;

			for (int n = 0; n < drawDataPtr.CmdListsCount; n += 1)
			{
				var cmdList = drawDataPtr.CmdLists[n];

				for (int cmdIndex = 0; cmdIndex < cmdList.CmdBuffer.Size; cmdIndex += 1)
				{
					var drawCmd = cmdList.CmdBuffer[cmdIndex];

					renderPass.BindFragmentSamplers(
						new TextureSamplerBinding(TextureStorage.GetTexture(drawCmd.TextureId), ImGuiSampler)
					);

					var topLeft = Vector2.Transform(new Vector2(drawCmd.ClipRect.X, drawCmd.ClipRect.Y), viewProjectionMatrix);
					var bottomRight = Vector2.Transform(new Vector2(drawCmd.ClipRect.Z, drawCmd.ClipRect.W), viewProjectionMatrix);

					var width = drawCmd.ClipRect.Z - (int)drawCmd.ClipRect.X;
					var height = drawCmd.ClipRect.W - (int)drawCmd.ClipRect.Y;

					if (width <= 0 || height <= 0)
					{
						continue;
					}

					renderPass.SetScissor(
						new Rect(
							(int)drawCmd.ClipRect.X,
							(int)drawCmd.ClipRect.Y,
							(int)width,
							(int)height
						)
					);

					renderPass.DrawIndexedPrimitives(
						drawCmd.ElemCount,
						1,
						indexOffset,
                        (int)vertexOffset,
						0
					);

					indexOffset += drawCmd.ElemCount;
				}

				vertexOffset += (uint)cmdList.VtxBuffer.Size;
			}

			commandBuffer.EndRenderPass(renderPass);
		}

		public struct Position2DTextureColorVertex : IVertexType
		{
			public Vector2 Position;
			public Vector2 TexCoord;
			public Color Color;

			public Position2DTextureColorVertex(
				Vector2 position,
				Vector2 texcoord,
				Color color
			)
			{
				Position = position;
				TexCoord = texcoord;
				Color = color;
			}

			public static VertexElementFormat[] Formats =>
            [
                VertexElementFormat.Float2,
				VertexElementFormat.Float2,
				VertexElementFormat.Ubyte4Norm
			];

			public static uint[] Offsets =>
			[
				0,
				8,
				16
			];
		}

		public class DebugTextureStorage
		{
			Dictionary<IntPtr, WeakReference<Texture>> PointerToTexture = new Dictionary<IntPtr, WeakReference<Texture>>();

			public IntPtr Add(Texture texture)
			{
				if (!PointerToTexture.ContainsKey(texture.Handle))
				{
					PointerToTexture.Add(texture.Handle, new WeakReference<Texture>(texture));
				}
				return texture.Handle;
			}

			public Texture GetTexture(IntPtr pointer)
			{
				if (!PointerToTexture.ContainsKey(pointer))
				{
					return null;
				}

				var result = PointerToTexture[pointer];

				if (!result.TryGetTarget(out var texture))
				{
					PointerToTexture.Remove(pointer);
					return null;
				}

				return texture;
			}
		}
	}
}
