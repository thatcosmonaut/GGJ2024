using System.Collections.Generic;
using System.IO;
using GGJ2024.Components;
using GGJ2024.Content;
using MoonTools.ECS;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Graphics.Font;
using MoonWorks.Math.Float;

namespace GGJ2024;

public class Renderer : MoonTools.ECS.Renderer
{
	GraphicsDevice GraphicsDevice;
	GraphicsPipeline SpriteBatchPipeline;
	GraphicsPipeline TextPipeline;

	SpriteBatch RectangleSpriteBatch;
	SpriteBatch ArtSpriteBatch;

	Texture RectangleAtlasTexture;
	Texture SpriteAtlasTexture;

	Sampler PointSampler;
	MoonTools.ECS.Filter RectangleFilter;
	MoonTools.ECS.Filter TextFilter;
	MoonTools.ECS.Filter SpriteAnimationFilter;

	Queue<TextBatch> BatchPool = new Queue<TextBatch>();
	List<(TextBatch, Matrix4x4)> ActiveBatchTransforms = new List<(TextBatch, Matrix4x4)>();

	public Renderer(World world, GraphicsDevice graphicsDevice, TextureFormat swapchainFormat) : base(world)
	{
		GraphicsDevice = graphicsDevice;

		RectangleFilter = FilterBuilder.Include<Rectangle>().Include<Position>().Build();
		TextFilter = FilterBuilder.Include<Text>().Include<Position>().Build();
		SpriteAnimationFilter = FilterBuilder.Include<SpriteAnimation>().Include<Position>().Build();

		var baseContentPath = Path.Combine(
			System.AppContext.BaseDirectory,
			"Content"
		);

		var shaderContentPath = Path.Combine(
			baseContentPath,
			"Shaders"
		);

		var commandBuffer = GraphicsDevice.AcquireCommandBuffer();

		RectangleAtlasTexture = Texture.CreateTexture2D(GraphicsDevice, 1, 1, TextureFormat.R8G8B8A8, TextureUsageFlags.Sampler);
		commandBuffer.SetTextureData(RectangleAtlasTexture, new Color[] { Color.White });
		GraphicsDevice.Submit(commandBuffer);

		SpriteAtlasTexture = TextureAtlases.TP_Sprites.Texture;

		var vertShaderModule = new ShaderModule(GraphicsDevice, Path.Combine(shaderContentPath, "InstancedSpriteBatch.vert.refresh"));
		var fragShaderModule = new ShaderModule(GraphicsDevice, Path.Combine(shaderContentPath, "InstancedSpriteBatch.frag.refresh"));

		SpriteBatchPipeline = new GraphicsPipeline(
			GraphicsDevice,
				new GraphicsPipelineCreateInfo
				{
					AttachmentInfo = new GraphicsPipelineAttachmentInfo(
						new ColorAttachmentDescription(
							swapchainFormat,
							ColorAttachmentBlendState.NonPremultiplied
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

		TextPipeline = new GraphicsPipeline(
			GraphicsDevice,
			new GraphicsPipelineCreateInfo
			{
				AttachmentInfo = new GraphicsPipelineAttachmentInfo(
					new ColorAttachmentDescription(
						swapchainFormat,
						ColorAttachmentBlendState.AlphaBlend
					)
				),
				DepthStencilState = DepthStencilState.Disable,
				VertexShaderInfo = GraphicsDevice.TextVertexShaderInfo,
				FragmentShaderInfo = GraphicsDevice.TextFragmentShaderInfo,
				VertexInputState = GraphicsDevice.TextVertexInputState,
				RasterizerState = RasterizerState.CCW_CullNone,
				PrimitiveType = PrimitiveType.TriangleList,
				MultisampleState = MultisampleState.None
			}
		);

		PointSampler = new Sampler(GraphicsDevice, SamplerCreateInfo.PointClamp);

		RectangleSpriteBatch = new SpriteBatch(GraphicsDevice);
		ArtSpriteBatch = new SpriteBatch(GraphicsDevice);
	}

	public void Render(Window window)
	{
		var commandBuffer = GraphicsDevice.AcquireCommandBuffer();

		var swapchainTexture = commandBuffer.AcquireSwapchainTexture(window);

		if (swapchainTexture != null)
		{
			RectangleSpriteBatch.Reset();
			ArtSpriteBatch.Reset();

			foreach (var (batch, _) in ActiveBatchTransforms)
			{
				FreeTextBatch(batch);
			}
			ActiveBatchTransforms.Clear();

			foreach (var entity in RectangleFilter.Entities)
			{
				var position = Get<Position>(entity);
				var rectangle = Get<Rectangle>(entity);
				var orientation = Has<Orientation>(entity) ? Get<Orientation>(entity).Angle : 0.0f;
				var color = Has<Color>(entity) ? Get<Color>(entity) : Color.White;

				RectangleSpriteBatch.Add(new Vector3(position.X, position.Y, -2f), orientation, new Vector2(rectangle.Width, rectangle.Height), color, new Vector2(0, 0), new Vector2(1, 1));
			}

			foreach (var entity in SpriteAnimationFilter.Entities)
			{
				var position = Get<Position>(entity);
				var sprite = Get<SpriteAnimation>(entity).CurrentSprite;

				ArtSpriteBatch.Add(new Vector3(position.X, position.Y, -1f), 0, new Vector2(sprite.SliceRect.W, sprite.SliceRect.H), Color.White, sprite.UV.LeftTop, sprite.UV.Dimensions);
			}

			foreach (var entity in TextFilter.Entities)
			{
				var text = Get<Text>(entity);
				var position = Get<Position>(entity);

				var str = Data.TextStorage.GetString(text.TextID);
				var font = Fonts.FromID(text.FontID);
				var color = Color.White;

				if (Has<ColorBlend>(entity))
				{
					color = Get<ColorBlend>(entity).Color;
				}

				var textBatch = AcquireTextBatch();
				textBatch.Start(font);
				ActiveBatchTransforms.Add((textBatch, Matrix4x4.CreateTranslation(new Vector3(position.X, position.Y, -1))));

				textBatch.Add(
					str,
					text.Size,
					color,
					text.HorizontalAlignment,
					text.VerticalAlignment
				);
			}

			if (RectangleSpriteBatch.InstanceCount > 0)
			{
				RectangleSpriteBatch.Upload(commandBuffer);
			}

			if (ArtSpriteBatch.InstanceCount > 0)
			{
				ArtSpriteBatch.Upload(commandBuffer);
			}

			foreach (var (batch, _) in ActiveBatchTransforms)
			{
				batch.UploadBufferData(commandBuffer);
			}

			commandBuffer.BeginRenderPass(
				new ColorAttachmentInfo(swapchainTexture, Color.Black)
			);

			var viewProjectionMatrices = new ViewProjectionMatrices(GetCameraMatrix(), GetProjectionMatrix());

			if (RectangleSpriteBatch.InstanceCount > 0)
			{
				RectangleSpriteBatch.Render(commandBuffer, SpriteBatchPipeline, RectangleAtlasTexture, PointSampler, viewProjectionMatrices);
			}

			if (ArtSpriteBatch.InstanceCount > 0)
			{
				ArtSpriteBatch.Render(commandBuffer, SpriteBatchPipeline, SpriteAtlasTexture, PointSampler, viewProjectionMatrices);
			}

			if (ActiveBatchTransforms.Count > 0)
			{
				commandBuffer.BindGraphicsPipeline(TextPipeline);
				foreach (var (batch, transform) in ActiveBatchTransforms)
				{
					batch.Render(commandBuffer, transform * viewProjectionMatrices.View * viewProjectionMatrices.Projection);
				}
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

	private TextBatch AcquireTextBatch()
	{
		if (BatchPool.Count > 0)
		{
			return BatchPool.Dequeue();
		}
		else
		{
			return new TextBatch(GraphicsDevice);
		}
	}

	private void FreeTextBatch(TextBatch batch)
	{
		BatchPool.Enqueue(batch);
	}
}
