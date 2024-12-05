using System.Collections.Generic;
using RollAndCash.Components;
using RollAndCash.Content;
using MoonTools.ECS;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Graphics.Font;
using System.Numerics;
using RollAndCash.Relations;

namespace RollAndCash;

public class Renderer : MoonTools.ECS.Renderer
{
	GraphicsDevice GraphicsDevice;
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

		RenderTexture = Texture.Create2D(GraphicsDevice, Dimensions.GAME_W, Dimensions.GAME_H, swapchainFormat, TextureUsageFlags.ColorTarget | TextureUsageFlags.Sampler);
		DepthTexture = Texture.Create2D(GraphicsDevice, Dimensions.GAME_W, Dimensions.GAME_H, TextureFormat.D16Unorm, TextureUsageFlags.DepthStencilTarget);

		SpriteAtlasTexture = TextureAtlases.TP_Sprites.Texture;

		TextPipeline = GraphicsPipeline.Create(
			GraphicsDevice,
			new GraphicsPipelineCreateInfo
			{
				TargetInfo = new GraphicsPipelineTargetInfo
				{
					DepthStencilFormat = TextureFormat.D16Unorm,
					HasDepthStencilTarget = true,
					ColorTargetDescriptions =
					[
						new ColorTargetDescription
						{
							Format = swapchainFormat,
							BlendState = ColorTargetBlendState.PremultipliedAlphaBlend
						}
					]
				},
				DepthStencilState = new DepthStencilState
				{
					EnableDepthTest = true,
					EnableDepthWrite = true,
					CompareOp = CompareOp.LessOrEqual
				},
				VertexShader = GraphicsDevice.TextVertexShader,
				FragmentShader = GraphicsDevice.TextFragmentShader,
				VertexInputState = GraphicsDevice.TextVertexInputState,
				RasterizerState = RasterizerState.CCW_CullNone,
				PrimitiveType = PrimitiveType.TriangleList,
				MultisampleState = MultisampleState.None
			}
		);

		PointSampler = Sampler.Create(GraphicsDevice, SamplerCreateInfo.PointClamp);

		ArtSpriteBatch = new SpriteBatch(GraphicsDevice, swapchainFormat, TextureFormat.D16Unorm);
	}

	public void Render(Window window)
	{
		var commandBuffer = GraphicsDevice.AcquireCommandBuffer();

		var swapchainTexture = commandBuffer.AcquireSwapchainTexture(window);

		if (swapchainTexture != null)
		{
			foreach (var (batch, _) in ActiveBatchTransforms)
			{
				FreeTextBatch(batch);
			}
			ActiveBatchTransforms.Clear();

			ArtSpriteBatch.Start();

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
				if (HasOutRelation<DontDraw>(entity))
					continue;

				var position = Get<Position>(entity);
				var animation = Get<SpriteAnimation>(entity);
				var sprite = animation.CurrentSprite;
				var origin = animation.Origin;
				var depth = -1f;
				var color = Color.White;

				Vector2 scale = Vector2.One;
				if (Has<SpriteScale>(entity))
				{
					scale *= Get<SpriteScale>(entity).Scale;
					origin *= scale;
				}

				var offset = -origin - new Vector2(sprite.FrameRect.X, sprite.FrameRect.Y) * scale;

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

				ArtSpriteBatch.Add(new Vector3(position.X + offset.X, position.Y + offset.Y, depth), 0, new Vector2(sprite.SliceRect.W, sprite.SliceRect.H) * scale, color, sprite.UV.LeftTop, sprite.UV.Dimensions);
			}

			foreach (var entity in TextFilter.Entities)
			{
				if (HasOutRelation<DontDraw>(entity))
					continue;

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

			ArtSpriteBatch.Upload(commandBuffer);

			foreach (var (batch, _) in ActiveBatchTransforms)
			{
				batch.UploadBufferData(commandBuffer);
			}

			var renderPass = commandBuffer.BeginRenderPass(
				new DepthStencilTargetInfo(DepthTexture, 1, 0),
				new ColorTargetInfo(RenderTexture, Color.Black)
			);

			var viewProjectionMatrices = new ViewProjectionMatrices(GetCameraMatrix(), GetProjectionMatrix());

			if (ArtSpriteBatch.InstanceCount > 0)
			{
				ArtSpriteBatch.Render(renderPass, SpriteAtlasTexture, PointSampler, viewProjectionMatrices);
			}

			if (ActiveBatchTransforms.Count > 0)
			{
				renderPass.BindGraphicsPipeline(TextPipeline);
				foreach (var (batch, transform) in ActiveBatchTransforms)
				{
					batch.Render(renderPass, transform * viewProjectionMatrices.View * viewProjectionMatrices.Projection);
				}
			}

			commandBuffer.EndRenderPass(renderPass);

			commandBuffer.Blit(RenderTexture, swapchainTexture, MoonWorks.Graphics.Filter.Nearest);
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
