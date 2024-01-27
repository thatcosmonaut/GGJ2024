using MoonWorks.Graphics;
using MoonWorks.Math.Float;

namespace GGJ2024;

public class SpriteBatch
{
	GraphicsDevice GraphicsDevice;
	SpriteInstanceData[] InstanceDatas;
	uint Index;

	public uint InstanceCount => Index;

	Buffer InstanceBuffer;
	Buffer QuadVertexBuffer;
	Buffer QuadIndexBuffer;

	public unsafe SpriteBatch(GraphicsDevice graphicsDevice)
	{
		GraphicsDevice = graphicsDevice;
		InstanceBuffer = Buffer.Create<SpriteInstanceData>(GraphicsDevice, BufferUsageFlags.Vertex, 1024);
		InstanceDatas = new SpriteInstanceData[1024];
		Index = 0;

		QuadVertexBuffer = Buffer.Create<PositionVertex>(GraphicsDevice, BufferUsageFlags.Vertex, 4);
		QuadIndexBuffer = Buffer.Create<ushort>(GraphicsDevice, BufferUsageFlags.Index, 6);

		var vertices = stackalloc PositionVertex[4];
		vertices[0].Position = new Vector3(0, 0, 0);
		vertices[1].Position = new Vector3(1, 0, 0);
		vertices[2].Position = new Vector3(0, 1, 0);
		vertices[3].Position = new Vector3(1, 1, 0);

		var indices = stackalloc ushort[6]
		{
			0, 1, 2,
			2, 1, 3
		};

		var commandBuffer = GraphicsDevice.AcquireCommandBuffer();
		commandBuffer.SetBufferData(QuadVertexBuffer, new System.Span<PositionVertex>(vertices, 4));
		commandBuffer.SetBufferData(QuadIndexBuffer, new System.Span<ushort>(indices, 6));
		GraphicsDevice.Submit(commandBuffer);
	}

	// Call this to reset so you can add new data to the batch
	public void Reset()
	{
		Index = 0;
	}

	// Add a sprite to the batch
	public void Add(
		Vector3 position,
		float rotation,
		Vector2 size,
		Color color,
		Vector2 leftTopUV,
		Vector2 dimensionsUV
	) {
		var left = leftTopUV.X;
		var top = leftTopUV.Y;
		var right = leftTopUV.X + dimensionsUV.X;
		var bottom = leftTopUV.Y + dimensionsUV.Y;

		InstanceDatas[Index].Translation = position;
		InstanceDatas[Index].Rotation = rotation;
		InstanceDatas[Index].Scale = size;
		InstanceDatas[Index].Color = color;
		InstanceDatas[Index].UV0 = leftTopUV;
		InstanceDatas[Index].UV1 = new Vector2(left, bottom);
		InstanceDatas[Index].UV2 = new Vector2(right, top);
		InstanceDatas[Index].UV3 = new Vector2(right, bottom);
		Index += 1;
	}

	// Call this outside of render pass
	public void Upload(CommandBuffer commandBuffer)
	{
		commandBuffer.SetBufferData(InstanceBuffer, InstanceDatas, 0, 0, Index);
	}

	// Call this inside of render pass
	public void Render(CommandBuffer commandBuffer, GraphicsPipeline pipeline, Texture texture, Sampler sampler, ViewProjectionMatrices viewProjectionMatrices)
	{
		commandBuffer.BindGraphicsPipeline(pipeline);
		commandBuffer.BindFragmentSamplers(new TextureSamplerBinding(texture, sampler));
		commandBuffer.BindVertexBuffers(
			new BufferBinding(QuadVertexBuffer, 0),
			new BufferBinding(InstanceBuffer, 0)
		);
		commandBuffer.BindIndexBuffer(QuadIndexBuffer, IndexElementSize.Sixteen);
		var vertParamOffset = commandBuffer.PushVertexShaderUniforms(viewProjectionMatrices);
		commandBuffer.DrawInstancedPrimitives(0, 0, 2, InstanceCount, vertParamOffset, 0);
	}
}

public struct PositionVertex : IVertexType
{
	public Vector3 Position;

	public static VertexElementFormat[] Formats =>
	[
		VertexElementFormat.Vector3
	];
}

public struct SpriteInstanceData : IVertexType
{
	public Vector3 Translation;
	public float Rotation;
	public Vector2 Scale;
	public Color Color;
	public Vector2 UV0;
	public Vector2 UV1;
	public Vector2 UV2;
	public Vector2 UV3;

	public static VertexElementFormat[] Formats =>
	[
		VertexElementFormat.Vector3,
		VertexElementFormat.Float,
		VertexElementFormat.Vector2,
		VertexElementFormat.Color,
		VertexElementFormat.Vector2,
		VertexElementFormat.Vector2,
		VertexElementFormat.Vector2,
		VertexElementFormat.Vector2
	];
}

public readonly record struct ViewProjectionMatrices(Matrix4x4 View, Matrix4x4 Projection);
