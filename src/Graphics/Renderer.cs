using System.IO;
using GGJ2024.Components;
using MoonTools.ECS;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;

namespace GGJ2024;

public class Renderer : MoonTools.ECS.Renderer
{
	GraphicsDevice GraphicsDevice;
	GraphicsPipeline SpriteBatchPipeline;

	SpriteBatch SpriteBatch;

	Texture SpriteAtlasTexture; // TODO: create this!
	Sampler PointSampler;
	MoonTools.ECS.Filter RectangleFilter;

	public Renderer(World world, GraphicsDevice graphicsDevice, TextureFormat swapchainFormat) : base(world)
	{
		GraphicsDevice = graphicsDevice;

		RectangleFilter = FilterBuilder.Include<Rectangle>().Include<Position>().Build();

		var baseContentPath = Path.Combine(
			System.AppContext.BaseDirectory,
			"Content"
		);

		var shaderContentPath = Path.Combine(
			baseContentPath,
			"Shaders"
		);

		var buffer = GraphicsDevice.AcquireCommandBuffer();

		SpriteAtlasTexture =
			Texture.FromImageFile(
				graphicsDevice,
				buffer,
				Path.Combine(baseContentPath, "1x1.png")
			);

		GraphicsDevice.Submit(buffer);

		var vertShaderModule = new ShaderModule(GraphicsDevice, Path.Combine(shaderContentPath, "InstancedSpriteBatch.vert.refresh"));
		var fragShaderModule = new ShaderModule(GraphicsDevice, Path.Combine(shaderContentPath, "InstancedSpriteBatch.frag.refresh"));

		SpriteBatchPipeline = new GraphicsPipeline(
			GraphicsDevice,
				new GraphicsPipelineCreateInfo
				{
					AttachmentInfo = new GraphicsPipelineAttachmentInfo(
						new ColorAttachmentDescription(
							swapchainFormat,
							ColorAttachmentBlendState.Opaque
						)
					),
					DepthStencilState = DepthStencilState.Disable,
					MultisampleState = MultisampleState.None,
					PrimitiveType = PrimitiveType.TriangleList,
					RasterizerState = RasterizerState.CCW_CullNone,
					VertexInputState = new VertexInputState([
						VertexBindingAndAttributes.Create<PositionVertex>(0),
					VertexBindingAndAttributes.Create<SpriteInstanceData>(1, 1, VertexInputRate.Instance)
					]),
					VertexShaderInfo = GraphicsShaderInfo.Create<ViewProjectionMatrices>(vertShaderModule, "main", 0),
					FragmentShaderInfo = GraphicsShaderInfo.Create(fragShaderModule, "main", 1)
				}
			);

		PointSampler = new Sampler(GraphicsDevice, SamplerCreateInfo.PointClamp);

		SpriteBatch = new SpriteBatch(GraphicsDevice);
	}

	public void Render(Window window)
	{
		var commandBuffer = GraphicsDevice.AcquireCommandBuffer();

		var swapchainTexture = commandBuffer.AcquireSwapchainTexture(window);

		if (swapchainTexture != null)
		{
			SpriteBatch.Reset();

			foreach (var entity in RectangleFilter.Entities)
			{
				var position = Get<Position>(entity);
				var rectangle = Get<Rectangle>(entity);
				var orientation = Has<Orientation>(entity) ? Get<Orientation>(entity).Angle : 0.0f;

				SpriteBatch.Add(new Vector3(position.X, position.Y, -1.0f), orientation, new Vector2(rectangle.Width, rectangle.Height), Color.White, new Vector2(0, 0), new Vector2(1, 1));
			}

			if (RectangleFilter.Count > 0)
				SpriteBatch.Upload(commandBuffer);

			commandBuffer.BeginRenderPass(
				new ColorAttachmentInfo(swapchainTexture, Color.CornflowerBlue)
			);

			if (SpriteBatch.InstanceCount > 0)
			{
				var viewProjectionMatrices = new ViewProjectionMatrices(GetCameraMatrix(), GetProjectionMatrix());
				SpriteBatch.Render(commandBuffer, SpriteBatchPipeline, SpriteAtlasTexture, PointSampler, viewProjectionMatrices);
			}

			commandBuffer.EndRenderPass();
		}

		GraphicsDevice.Submit(commandBuffer);
	}

	public Matrix4x4 GetCameraMatrix()
	{
		return Matrix4x4.Identity;
	}

	public Matrix4x4 GetProjectionMatrix()
	{
		return Matrix4x4.CreateOrthographicOffCenter(
			0,
			Dimensions.GAME_W,
			Dimensions.GAME_H,
			0,
			0.01f,
			1000
		);
	}
}
