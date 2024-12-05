using MoonWorks.Graphics;
using System.Numerics;
using System.Runtime.InteropServices;
using Buffer = MoonWorks.Graphics.Buffer;

namespace RollAndCash;

public class SpriteBatch
{
	const int MAX_SPRITE_COUNT = 8192;

	GraphicsDevice GraphicsDevice;
	ComputePipeline ComputePipeline;
	GraphicsPipeline GraphicsPipeline;

	int InstanceIndex;
	public uint InstanceCount => (uint) InstanceIndex;

	TransferBuffer InstanceTransferBuffer;
	Buffer InstanceBuffer;
	Buffer QuadVertexBuffer;
	Buffer QuadIndexBuffer;

	public SpriteBatch(GraphicsDevice graphicsDevice, TextureFormat renderTextureFormat, TextureFormat? depthTextureFormat = null)
	{
		GraphicsDevice = graphicsDevice;

        var baseContentPath = System.IO.Path.Combine(
            System.AppContext.BaseDirectory,
            "Content"
        );

        var shaderContentPath = System.IO.Path.Combine(
            baseContentPath,
            "Shaders"
        );

        ComputePipeline = ShaderCross.Create(GraphicsDevice, System.IO.Path.Combine(shaderContentPath, "SpriteBatch.comp.hlsl.spv"), "main", ShaderCross.ShaderFormat.SPIRV);

		var vertShader = ShaderCross.Create(GraphicsDevice, System.IO.Path.Combine(shaderContentPath, "SpriteBatch.vert.hlsl.spv"), "main", ShaderCross.ShaderFormat.SPIRV, ShaderStage.Vertex);
		var fragShader = ShaderCross.Create(GraphicsDevice, System.IO.Path.Combine(shaderContentPath, "SpriteBatch.frag.hlsl.spv"), "main", ShaderCross.ShaderFormat.SPIRV, ShaderStage.Fragment);

		var createInfo = new GraphicsPipelineCreateInfo
		{
			TargetInfo = new GraphicsPipelineTargetInfo
			{
				ColorTargetDescriptions = [
					new ColorTargetDescription
					{
						Format = renderTextureFormat,
						BlendState = ColorTargetBlendState.NonPremultipliedAlphaBlend
					}
				]
			},
			DepthStencilState = DepthStencilState.Disable,
			MultisampleState = MultisampleState.None,
			PrimitiveType = PrimitiveType.TriangleList,
			RasterizerState = RasterizerState.CCW_CullNone,
			VertexInputState = VertexInputState.CreateSingleBinding<PositionTextureColorVertex>(),
			VertexShader = vertShader,
			FragmentShader = fragShader
		};

		if (depthTextureFormat.HasValue)
		{
			createInfo.TargetInfo.DepthStencilFormat = depthTextureFormat.Value;
			createInfo.TargetInfo.HasDepthStencilTarget = true;

			createInfo.DepthStencilState = new DepthStencilState
			{
				EnableDepthTest = true,
				EnableDepthWrite = true,
				CompareOp = CompareOp.LessOrEqual
			};
		}

        GraphicsPipeline = GraphicsPipeline.Create(
            GraphicsDevice,
            createInfo
        );

        fragShader.Dispose();
        vertShader.Dispose();

		InstanceTransferBuffer = TransferBuffer.Create<SpriteInstanceData>(GraphicsDevice, TransferBufferUsage.Upload, MAX_SPRITE_COUNT);
		InstanceBuffer = Buffer.Create<SpriteInstanceData>(GraphicsDevice, BufferUsageFlags.Vertex, MAX_SPRITE_COUNT);
		InstanceIndex = 0;

		TransferBuffer spriteIndexTransferBuffer = TransferBuffer.Create<uint>(
			GraphicsDevice,
			TransferBufferUsage.Upload,
			MAX_SPRITE_COUNT * 6
		);

		QuadVertexBuffer = Buffer.Create<PositionTextureColorVertex>(
			GraphicsDevice,
			BufferUsageFlags.ComputeStorageWrite | BufferUsageFlags.Vertex,
			MAX_SPRITE_COUNT * 4
		);

		QuadIndexBuffer = Buffer.Create<uint>(
			GraphicsDevice,
			BufferUsageFlags.Index,
			MAX_SPRITE_COUNT * 6
		);

		var indexSpan = spriteIndexTransferBuffer.Map<uint>(false);

		for (int i = 0, j = 0; i < MAX_SPRITE_COUNT * 6; i += 6, j += 4)
		{
			indexSpan[i]     =  (uint) j;
			indexSpan[i + 1] =  (uint) j + 1;
			indexSpan[i + 2] =  (uint) j + 2;
			indexSpan[i + 3] =  (uint) j + 3;
			indexSpan[i + 4] =  (uint) j + 2;
			indexSpan[i + 5] =  (uint) j + 1;
		}
		spriteIndexTransferBuffer.Unmap();

		var cmdbuf = GraphicsDevice.AcquireCommandBuffer();
		var copyPass = cmdbuf.BeginCopyPass();
		copyPass.UploadToBuffer(spriteIndexTransferBuffer, QuadIndexBuffer, false);
		cmdbuf.EndCopyPass(copyPass);
		GraphicsDevice.Submit(cmdbuf);
	}

	// Call this before adding sprites
	public void Start()
	{
		InstanceIndex = 0;
		InstanceTransferBuffer.Map(true);
	}

	// Add a sprite to the batch
	public void Add(
		Vector3 position,
		float rotation,
		Vector2 size,
		Color color,
		Vector2 leftTopUV,
		Vector2 dimensionsUV
	)
	{
		var left = leftTopUV.X;
		var top = leftTopUV.Y;
		var right = leftTopUV.X + dimensionsUV.X;
		var bottom = leftTopUV.Y + dimensionsUV.Y;

		var instanceDatas = InstanceTransferBuffer.MappedSpan<SpriteInstanceData>();
		instanceDatas[InstanceIndex].Translation = position;
		instanceDatas[InstanceIndex].Rotation = rotation;
		instanceDatas[InstanceIndex].Scale = size;
		instanceDatas[InstanceIndex].Color = color.ToVector4();
		instanceDatas[InstanceIndex].UV0 = leftTopUV;
		instanceDatas[InstanceIndex].UV1 = new Vector2(right, top);
		instanceDatas[InstanceIndex].UV2 = new Vector2(left, bottom);
		instanceDatas[InstanceIndex].UV3 = new Vector2(right, bottom);
		InstanceIndex += 1;
	}

	// Call this outside of any pass
	public void Upload(CommandBuffer commandBuffer)
	{
		InstanceTransferBuffer.Unmap();

		if (InstanceCount > 0)
		{
			var copyPass = commandBuffer.BeginCopyPass();
			copyPass.UploadToBuffer(new TransferBufferLocation(InstanceTransferBuffer), new BufferRegion(InstanceBuffer, 0, (uint)(Marshal.SizeOf<SpriteInstanceData>() * InstanceCount)), true);
			commandBuffer.EndCopyPass(copyPass);

			var computePass = commandBuffer.BeginComputePass(
				new StorageBufferReadWriteBinding(QuadVertexBuffer, true)
			);
			computePass.BindComputePipeline(ComputePipeline);
			computePass.BindStorageBuffers(InstanceBuffer);
			computePass.Dispatch((InstanceCount / 64) + 1, 1, 1);
			commandBuffer.EndComputePass(computePass);
		}
	}

	// Call this inside of render pass
	public void Render(RenderPass renderPass, Texture texture, Sampler sampler, ViewProjectionMatrices viewProjectionMatrices)
	{
		renderPass.BindGraphicsPipeline(GraphicsPipeline);
		renderPass.BindFragmentSamplers(new TextureSamplerBinding(texture, sampler));
		renderPass.BindVertexBuffers(new BufferBinding(QuadVertexBuffer, 0));
		renderPass.BindIndexBuffer(QuadIndexBuffer, IndexElementSize.ThirtyTwo);
		renderPass.CommandBuffer.PushVertexUniformData(viewProjectionMatrices.View * viewProjectionMatrices.Projection);
		renderPass.DrawIndexedPrimitives(InstanceCount * 6, 1, 0, 0, 0);
	}
}

[StructLayout(LayoutKind.Explicit, Size = 48)]
struct PositionTextureColorVertex : IVertexType
{
	[FieldOffset(0)]
	public Vector4 Position;

	[FieldOffset(16)]
	public Vector2 TexCoord;

	[FieldOffset(32)]
	public Vector4 Color;

	public static VertexElementFormat[] Formats { get; } =
	[
		VertexElementFormat.Float4,
		VertexElementFormat.Float2,
		VertexElementFormat.Float4
	];

	public static uint[] Offsets { get; } =
	[
		0,
		16,
		32
	];
}

[StructLayout(LayoutKind.Explicit, Size = 80)]
public record struct SpriteInstanceData
{
	[FieldOffset(0)]
	public Vector3 Translation;
	[FieldOffset(12)]
	public float Rotation;
	[FieldOffset(16)]
	public Vector2 Scale;
	[FieldOffset(32)]
	public Vector4 Color;
	[FieldOffset(48)]
	public Vector2 UV0;
	[FieldOffset(56)]
	public Vector2 UV1;
	[FieldOffset(64)]
	public Vector2 UV2;
	[FieldOffset(72)]
	public Vector2 UV3;
}

public readonly record struct ViewProjectionMatrices(Matrix4x4 View, Matrix4x4 Projection);
