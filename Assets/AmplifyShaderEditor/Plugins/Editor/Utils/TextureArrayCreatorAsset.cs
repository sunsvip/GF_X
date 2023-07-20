// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TextureArrayCreatorAsset : ScriptableObject
{
#pragma warning disable
	[SerializeField]
	private int m_selectedSize = 4;

	[SerializeField]
	private bool m_lockRatio = true;

	[SerializeField]
	private int m_sizeX = 512;

	[SerializeField]
	private int m_sizeY = 512;

	[SerializeField]
	private bool m_tex3DMode = false;

	[SerializeField]
	private bool m_linearMode = false;

	[SerializeField]
	private bool m_mipMaps = true;

	[SerializeField]
	private TextureWrapMode m_wrapMode = TextureWrapMode.Repeat;

	[SerializeField]
	private FilterMode m_filterMode = FilterMode.Bilinear;

	[SerializeField]
	private int m_anisoLevel = 1;

	[SerializeField]
	private TextureFormat m_selectedFormatEnum = TextureFormat.ARGB32;

	[SerializeField]
	private int m_quality = 100;

	[SerializeField]
	private string m_folderPath = "Assets/";

	[SerializeField]
	private string m_fileName = "NewTextureArray";

	[SerializeField]
	private bool m_filenameChanged = false;

	[SerializeField]
	private List<Texture2D> m_allTextures = new List<Texture2D>();

	public int SelectedSize { get { return m_selectedSize; } }
	public int SizeX { get { return m_sizeX; } }
	public int SizeY { get { return m_sizeY; } }
	public bool Tex3DMode { get { return m_tex3DMode; } }
	public bool LinearMode { get { return m_linearMode; } }
	public bool MipMaps { get { return m_mipMaps; } }
	public TextureWrapMode WrapMode { get { return m_wrapMode; } }
	public FilterMode FilterMode { get { return m_filterMode; } }
	public int AnisoLevel { get { return m_anisoLevel; } }
	public TextureFormat SelectedFormatEnum { get { return m_selectedFormatEnum; } }
	public int Quality { get { return m_quality; } }
	public string FolderPath { get { return m_folderPath; } }
	public string FileName { get { return m_fileName; } }
	public List<Texture2D> AllTextures { get { return m_allTextures; } }
#pragma warning restore
}
