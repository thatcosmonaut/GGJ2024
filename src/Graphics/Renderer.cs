using System.Collections.Generic;
using System.IO;
using RollAndCash.Components;
using RollAndCash.Content;
using MoonTools.ECS;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Graphics.Font;
using MoonWorks.Math.Float;
using System;

namespace RollAndCash;

public class Renderer : MoonTools.ECS.Renderer
{
	GraphicsDevice GraphicsDevice;
	GraphicsPipeline SpriteBatchPipeline;
	GraphicsPipeline TextPipeline;

	SpriteBatch ArtSpriteBatch;

	Texture RenderTexture;
	Texture DepthTexture;

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

		RectangleFilter = FilterBuilder.Include<Rectangle>().Include<Position>().Include<DrawAsRectangle>().Build();
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

		RenderTexture = Texture.CreateTexture2D(GraphicsDevice, Dimensions.GAME_W, Dimensions.GAME_H, swapchainFormat, TextureUsageFlags.ColorTarget);
		DepthTexture = Texture.CreateTexture2D(GraphicsDevice, Dimensions.GAME_W, Dimensions.GAME_H, TextureFormat.D16, TextureUsageFlags.DepthStencilTarget);

		SpriteAtlasTexture = TextureAtlases.TP_Sprites.Texture;

		var vertShaderModule = new ShaderModule(GraphicsDevice, Path.Combine(shaderContentPath, "InstancedSpriteBatch.vert.refresh"));
		var fragShaderModule = new ShaderModule(GraphicsDevice, Path.Combine(shaderContentPath, "InstancedSpriteBatch.frag.refresh"));

		SpriteBatchPipeline = new GraphicsPipeline(
			GraphicsDevice,
				new GraphicsPipelineCreateInfo
				{
					AttachmentInfo = new GraphicsPipelineAttachmentInfo(
						TextureFormat.D16,
						new ColorAttachmentDescription(
							swapchainFormat,
							ColorAttachmentBlendState.NonPremultiplied
						)
					),
					DepthStencilState = DepthStencilState.DepthReadWrite,
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
					TextureFormat.D16,
					new ColorAttachmentDescription(
						swapchainFormat,
						ColorAttachmentBlendState.AlphaBlend
					)
				),
				DepthStencilState = DepthStencilState.DepthReadWrite,
				VertexShaderInfo = GraphicsDevice.TextVertexShaderInfo,
				FragmentShaderInfo = GraphicsDevice.TextFragmentShaderInfo,
				VertexInputState = GraphicsDevice.TextVertexInputState,
				RasterizerState = RasterizerState.CCW_CullNone,
				PrimitiveType = PrimitiveType.TriangleList,
				MultisampleState = MultisampleState.None
			}
		);

		PointSampler = new Sampler(GraphicsDevice, SamplerCreateInfo.PointClamp);

		ArtSpriteBatch = new SpriteBatch(GraphicsDevice);
	}

	public void Render(Window window)
	{
		var commandBuffer = GraphicsDevice.AcquireCommandBuffer();

		var swapchainTexture = commandBuffer.AcquireSwapchainTexture(window);

		if (swapchainTexture != null)
		{
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
				var color = Has<ColorBlend>(entity) ? Get<ColorBlend>(entity).Color : Color.White;
				var depth = -2f;
				if (Has<Depth>(entity))
				{
					depth = -Get<Depth>(entity).Value;
				}

				var sprite = SpriteAnimations.Pixel.Frames[0];
				ArtSpriteBatch.Add(new Vector3(position.X + rectangle.X, position.Y + rectangle.Y, depth), orientation, new Vector2(rectangle.Width, rectangle.Height), color, sprite.UV.LeftTop, sprite.UV.Dimensions);
			}

			foreach (var entity in SpriteAnimationFilter.Entities)
			{
				var position = Get<Position>(entity);
				var animation = Get<SpriteAnimation>(entity);
				var sprite = animation.CurrentSprite;
				var origin = animation.Origin;
				var offset = -origin - new Vector2(sprite.FrameRect.X, sprite.FrameRect.Y);
				var depth = -1f;
				var color = Color.White;

				if (Has<ColorBlend>(entity))
				{
					color = Get<ColorBlend>(entity).Color;
				}

				if (Has<ColorFlicker>(entity))
				{
					var colorFlicker = Get<ColorFlicker>(entity);
					if (colorFlicker.ElapsedFrames % 2 == 0)
					{
						color = colorFlicker.Color;
					}
				}

				if (Has<Depth>(entity))
				{
					depth = -Get<Depth>(entity).Value;
				}

				ArtSpriteBatch.Add(new Vector3(position.X + offset.X, position.Y + offset.Y, depth), 0, new Vector2(sprite.SliceRect.W, sprite.SliceRect.H), color, sprite.UV.LeftTop, sprite.UV.Dimensions);
			}

			foreach (var entity in TextFilter.Entities)
			{
				var text = Get<Text>(entity);
				var position = Get<Position>(entity);

				var str = Data.TextStorage.GetString(text.TextID);
				var font = Fonts.FromID(text.FontID);
				var color = Has<Color>(entity) ? Get<Color>(entity) : Color.White;
				var depth = -1f;

				if (Has<ColorBlend>(entity))
				{
					color = Get<ColorBlend>(entity).Color;
				}

				if (Has<Depth>(entity))
				{
					depth = -Get<Depth>(entity).Value;
				}

				if (Has<TextDropShadow>(entity))
				{
					var dropShadow = Get<TextDropShadow>(entity);

					var dropShadowPosition = position + new Position(dropShadow.OffsetX, dropShadow.OffsetY);

					var dropShadowBatch = AcquireTextBatch();
					dropShadowBatch.Start(font);
					ActiveBatchTransforms.Add((dropShadowBatch, Matrix4x4.CreateTranslation(dropShadowPosition.X, dropShadowPosition.Y, depth - 1)));

					dropShadowBatch.Add(
						str,
						text.Size,
						new Color(0, 0, 0, color.A),
						text.HorizontalAlignment,
						text.VerticalAlignment
					);
				}

				var textBatch = AcquireTextBatch();
				textBatch.Start(font);
				ActiveBatchTransforms.Add((textBatch, Matrix4x4.CreateTranslation(new Vector3(position.X, position.Y, depth))));

				textBatch.Add(
					str,
					text.Size,
					color,
					text.HorizontalAlignment,
					text.VerticalAlignment
				);

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
				new DepthStencilAttachmentInfo(DepthTexture, new DepthStencilValue(1, 0)),
				new ColorAttachmentInfo(RenderTexture, Color.Black)
			);

			var viewProjectionMatrices = new ViewProjectionMatrices(GetCameraMatrix(), GetProjectionMatrix());

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

			commandBuffer.CopyTextureToTexture(RenderTexture, swapchainTexture, MoonWorks.Graphics.Filter.Nearest);
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
