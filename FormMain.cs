using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Drawing.Drawing2D;
using System.Collections;

namespace CssSprite
{
    public partial class FormMain : Form
    {
        private List<ImageInfo> _imgList;
        private string dialogFile = string.Empty;
        private string basePath;
        internal class ImageInfo
        {
            internal ImageInfo(Image img, string name, string fileName)
            {
                Image = img;
                Name = name;
                FileName = fileName;
            }

            internal readonly Image Image;
            internal readonly string Name;
            internal readonly string FileName;
        }

        public FormMain()
        {
            InitializeComponent();
            this.Resize += FormMain_Resize;
            resize();
            panelImages.MouseWheel += panelImages_MouseWheel;
            panelImages.MouseHover += panelImages_MouseHover;
        }

        void panelImages_MouseHover(object sender, EventArgs e)
        {
            panelImages.Focus();
        }

        void panelImages_MouseWheel(object sender, MouseEventArgs e)
        {
            panelImages.ResumeLayout(false);
            panelImages.Refresh();
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            comboBoxBgColor.DataSource = Enum.GetNames(typeof(KnownColor));
            comboBoxBgColor.Text = "Transparent";
        }

        void FormMain_Resize(object sender, EventArgs e)
        {
            resize();
        }

        private void resize()
        {
            txtCss.Width = txtSass.Width = this.Width-20;
        }


        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            if (!OpenFile(false)) {
                return;
            }
            DialogResult dr = openFileDialog.ShowDialog();
            if (DialogResult.OK == dr && openFileDialog.FileNames.Length > 0)
            {
                if (!AssertFiles())
                {
                    return;
                }
                basePath = Path.GetDirectoryName(openFileDialog.FileName);
                folderBrowserDialog.SelectedPath = basePath;
                LoadImages(openFileDialog.FileNames);
                ButtonVRange_Click(null, EventArgs.Empty);
            }
        }

        private void btnSprite_Click(object sender, EventArgs e)
        {
            if (!OpenFile(true))
            {
                return;
            }
            DialogResult dr = openFileDialog.ShowDialog();
            if (DialogResult.OK == dr && openFileDialog.FileNames.Length > 0)
            {
                basePath = Path.GetDirectoryName(openFileDialog.FileName);
                folderBrowserDialog.SelectedPath = basePath;
                var spriteFile=new SpriteFile();
                try
                {
                    spriteFile = (SpriteFile)XmlSerializer.LoadFromXml(openFileDialog.FileNames[0], spriteFile.GetType());
                    if (_imgList == null)
                    {
                        _imgList = new List<ImageInfo>();
                    }
                    var noFile = "��Щ�ļ������ڣ�" + Environment.NewLine;
                    var hasFile=false;
                    foreach (Sprite s in spriteFile.SpriteList)
                    {
                        var path=folderBrowserDialog.SelectedPath+"\\"+ s.Path;
                        if (File.Exists(path))
                        {
                            Image img = Image.FromFile(path);
                            string imgName = Path.GetFileNameWithoutExtension(s.Path);
                            ImageInfo imgInfo = new ImageInfo(img, imgName, s.Path);
                            img.Tag = imgInfo;
                            _imgList.Add(imgInfo);
                            AddPictureBox(img, s.LocationX, s.LocationY);
                        }
                        else 
                        {
                            hasFile=true;
                            noFile += path + Environment.NewLine;
                        }
                    }
                    if (hasFile) 
                    {
                        MessageBox.Show(noFile, "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    txtDir.Text = spriteFile.CssFileName;
                    txtName.Text = spriteFile.ImageName;
                    chkBoxPhone.Checked = spriteFile.IsPhone;
                    panelImages.ResumeLayout(false);
                    SetCssText();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message + Environment.NewLine + ".sprite�ļ����𻵣��޷��򿪣�");
                }
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            openFileDialog.Filter = "Png�ļ�|*.png|Jpeg�ļ�|*.jpeg|Jpg�ļ�|*.jpg";
            openFileDialog.Multiselect = false;
            DialogResult dr = openFileDialog.ShowDialog();
            if (DialogResult.OK == dr && openFileDialog.FileNames.Length > 0)
            {
                if (_imgList == null)
                {
                    _imgList = new List<ImageInfo>();
                }
                var fileName = openFileDialog.FileName;
                
                if (!IsImgExists(fileName))
                {
                    Image img = Image.FromFile(fileName);
                    string imgName = Path.GetFileNameWithoutExtension(fileName);
                    ImageInfo imgInfo = new ImageInfo(img, imgName, fileName);
                    img.Tag = imgInfo;
                    _imgList.Add(imgInfo);
                    AddPictureBox(img, 0, 0);
                }
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedPicture != null)
            { 
                var dr =  MessageBox.Show("ȷ��ɾ��ͼƬ��" + ((ImageInfo)_selectedPicture.Image.Tag).Name + " ��", "ѯ��", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dr == DialogResult.Yes) {
                    foreach (ImageInfo info in _imgList)
                    {
                        if (info.Image == _selectedPicture.Image)
                        {
                            _imgList.Remove(info);
                            break;
                        }
                    }
                    panelImages.Controls.Remove(_selectedPicture);
                    _selectedPicture = null;
                }
            }
            else
            {
                MessageBox.Show("��ѡ������Ҫ�Ƴ���ͼƬ��");
            }
        }

        /// <summary>
        /// �����Լ��Ի����ʼ��
        /// </summary>
        /// <param name="spriteFile"></param>
        private bool OpenFile(bool spriteFile) 
        {
            if (_imgList != null && _imgList.Count > 0)
            {
                DialogResult queryDr = MessageBox.Show("ȷʵҪ����ѡ��ͼƬ������ѡ��ͼƬ����ǰ��ͼƬ���ֽ���ʧ��", "ѯ��", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (queryDr == DialogResult.Yes)
                {
                    _imgList.Clear();
                    panelImages.Controls.Clear();
                }
                else
                {
                    return false ;
                }
            }
            if (spriteFile)
            {
                openFileDialog.Filter = "css sprite�ļ�|*.sprite";
                openFileDialog.Multiselect = false;
            }
            else {
                openFileDialog.Filter = "Png�ļ�|*.png|Jpeg�ļ�|*.jpeg|Jpg�ļ�|*.jpg";
                openFileDialog.Multiselect = true;
            }
            return true;
        }

        /// <summary>
        /// ����ͼƬ������
        /// </summary>
        /// <param name="imageFileNames"></param>
        private void LoadImages(string[] imageFileNames)
        {
            if (_imgList == null)
            {
                _imgList = new List<ImageInfo>();
            }
            foreach (string fileName in imageFileNames)
            {
                if (IsImgExists(fileName))
                {
                    continue;
                }
                Image img = Image.FromFile(fileName);
                string imgName = Path.GetFileNameWithoutExtension(fileName);
                ImageInfo imgInfo = new ImageInfo(img, imgName, fileName);
                img.Tag = imgInfo;
                _imgList.Add(imgInfo);
            }
            _imgList.Sort(ImageComparison);
        }

        int ImageComparison(ImageInfo i1, ImageInfo i2)
        {
            return i1.Image.Width > i2.Image.Width ? 1 : (i1.Image.Width == i2.Image.Width ? 0 : -1);
        }

        /// <summary>
        /// ��֤�Ƿ��Ƕ���ļ�
        /// </summary>
        /// <returns></returns>
        private bool AssertFiles()
        {
            string[] files = openFileDialog.FileNames;
            if (files == null || (openFileDialog.Multiselect ? files.Length < 2 : files.Length <0))
            {
                MessageBox.Show("��ѡ����ͼƬ�ļ���");
                return false;
            }
            return true;
        }

        private PictureBox _selectedPicture=null;
        private Size _bigSize;


        string GetImgExt()
        {
            string ext = comboBoxImgType.Text.ToLower();
            if (ext == "png" || ext == "gif" || ext == "jpg" || ext == "jpeg")
            {
                return ext;
            }
            return "png";
        }

        /// <summary>
        /// �õ�sass����
        /// </summary>
        /// <param name="img">ͼƬ</param>
        /// <param name="left">��߾���</param>
        /// <param name="top">�ұ߾���</param>
        /// <returns></returns>
        string GetSassCss(Image img, int left, int top) 
        {
            ImageInfo imgInfo = (ImageInfo)img.Tag;
            var isPhone = chkBoxPhone.Checked;
            if (isPhone) {
                left = left / 2;
                top = top / 2;
            }
            var _left = left == 0 ? "0" : (0 - left).ToString() + "px";
            var _top = top == 0 ? "0" : (0 - top).ToString() + "px";
            var imgHeight = isPhone ? img.Height / 2 : img.Height;
            var imgWidth = isPhone ? img.Width / 2 : img.Width;
            return "@mixin " + GetCssName(imgInfo.Name) + "{height:" + imgHeight + "px;width:" + imgWidth + "px;" + "background-position: " + _left + " " + _top + ";}" + Environment.NewLine;
        }


        /// <summary>
        /// ��ȡcss����
        /// </summary>
        /// <param name="img">ͼƬ</param>
        /// <param name="left">��߾���</param>
        /// <param name="top">�ұ߾���</param>
        /// <returns></returns>
        string GetCss(Image img, int left, int top)
        {
            ImageInfo imgInfo = (ImageInfo)img.Tag;
            var isPhone = chkBoxPhone.Checked;
            if (isPhone)
            {
                left = left / 2;
                top = top / 2;
            }
            var _left = left == 0 ? "0" : (0 - left).ToString() + "px";
            var _top = top == 0 ? "0" : (0 - top).ToString() + "px";
            var imgHeight = isPhone ? img.Height / 2 : img.Height;
            var imgWidth = isPhone ? img.Width / 2 : img.Width;
            return "." + GetCssName(imgInfo.Name) + "{height:" + imgHeight + "px;width:" + imgWidth + "px;background-position:" + _left + " " + _top + ";}" + Environment.NewLine;
        }

        string GetCssName(string imgName)
        {
            if (Char.IsNumber(imgName[0]))
            {
                return "_" + imgName;
            }
            return imgName;
        }

        
        //Сͼ���ŵ��
        private void ButtonVRange_Click(object sender, EventArgs e)
        {
            if (!AssertFiles()) return;
            panelImages.Controls.Clear();
            int left = 0;
            int top = 0;
            int currentHeight = 0;
            foreach (ImageInfo ii in _imgList)
            {
                Image img = ii.Image;
                left = 0;
                top = currentHeight;

                AddPictureBox(img, left, top);
                currentHeight += img.Height;
            }
            panelImages.ResumeLayout(false);
            SetCssText();
        }

        /// <summary>
        /// ����css���ı�
        /// </summary>
        public void SetCssText() {
            
            int maxWidth, maxHeight;
            maxWidth = maxHeight = 0;
            foreach (PictureBox pb in panelImages.Controls)
            {
                maxWidth = Math.Max(maxWidth, pb.Location.X + pb.Image.Width);
                maxHeight = Math.Max(maxHeight, pb.Location.Y + pb.Image.Height);
            }
            _bigSize = new Size(maxWidth, maxHeight);
            var isPhone = chkBoxPhone.Checked;
            var sassStr = "@mixin " + txtName.Text + "{background:url(" + txtDir.Text + "/" + txtName.Text + "." + GetImgExt() + ") no-repeat;" + (isPhone ? "background-size:" + _bigSize.Width / 2 + "px " + _bigSize.Height / 2 + "px" : "") + " }" + Environment.NewLine;
            var cssStr = "." + txtName.Text + "{background:url(" + txtDir.Text + "/" + txtName.Text + "." + GetImgExt() + ")  no-repeat;" + (isPhone ? "background-size:" + _bigSize.Width / 2 + "px " + _bigSize.Height / 2 + "px" : "") + "}" + Environment.NewLine;
            foreach (PictureBox pb in panelImages.Controls)
            {
                sassStr += GetSassCss(pb.Image, pb.Left, pb.Top);
                cssStr += GetCss(pb.Image, pb.Left, pb.Top);
            }
            txtSass.Text = sassStr;
            txtCss.Text = cssStr;
        }

        public string GetImgName(Image img)
        {
            foreach (ImageInfo ii in _imgList)
            {
                if (ii.Image == img)
                {
                    return ii.Name;
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// ����ͼƬ
        /// </summary>
        /// <param name="img">ͼƬ</param>
        /// <param name="left">���</param>
        /// <param name="top">�ϱ�</param>
        private void AddPictureBox(Image img, int left, int top)
        {
            PictureBox pb = new PictureBox();
            pb.Image = img;
            pb.Location = new System.Drawing.Point(left, top);
            pb.Cursor = Cursors.SizeAll;
            pb.BorderStyle =BorderStyle.FixedSingle ;
            pb.Name = "pb_" + left + "_" + top;
            pb.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            //pb.Click += pb_Click;
            pb.MouseDown += pb_MouseDown;
            pb.MouseMove += pb_MouseMove;
            pb.MouseUp += pb_MouseUp;
            panelImages.Controls.Add(pb);
            pb.Show();
        }

        #region �϶�
        bool _isDragged = false;
        Point _dragStartLocation;
        void pb_MouseUp(object sender, MouseEventArgs e)
        {
            _isDragged = false;
        }

        void pb_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragged)
            {
                PictureBox pb = sender as PictureBox;
                Point p = e.Location;
                int x = Math.Max(0, pb.Location.X + p.X - _dragStartLocation.X);
                int y = Math.Max(0, pb.Location.Y + p.Y - _dragStartLocation.Y);
                pb.Location = new Point(x, y);
                panelImages.ResumeLayout(false);
                SetCssText();
            }
        }

        void pb_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _selectedPicture = (PictureBox)sender;
                _isDragged = true;
                _dragStartLocation = new Point(e.X, e.Y);
            }
            else
            {
                _isDragged = false;
            }
        }


        #endregion

        void pb_Click(object sender, EventArgs e)
        {
            PictureBox pb = (PictureBox)sender;
            if (_selectedPicture != null)
            {
                _selectedPicture.BorderStyle = BorderStyle.None;
            }
            pb.BorderStyle = BorderStyle.FixedSingle;
        }

        private void ButtonMakeBigImageCss_Click(object sender, EventArgs e)
        {
            panelImages.VerticalScroll.Value=0 ;
            panelImages.HorizontalScroll.Value = 0;
            if (_imgList == null || _imgList.Count < 2)
            {
                MessageBox.Show("��ѡ��������ͼƬ��");
                return;
            }

            DialogResult dr = folderBrowserDialog.ShowDialog();
            if (dr == DialogResult.OK)
            {
                string imgDir = folderBrowserDialog.SelectedPath;
                if (!Directory.Exists(imgDir))
                {
                    Directory.CreateDirectory(imgDir);
                }
                string imgPath = Path.Combine(imgDir, txtName.Text+"."+GetImgExt());
                if (File.Exists(imgPath))
                {
                    if (DialogResult.Yes !=
                        MessageBox.Show("ѡ���ļ������Ѵ���" + txtName.Text + "." + GetImgExt() + "������ִ�н������Ѵ����ļ����Ƿ������", "ѯ��"
                        , MessageBoxButtons.YesNo, MessageBoxIcon.Question))
                    {
                        return;
                    }
                }

                int maxWidth,maxHeight,minWidth,minHeight;
                maxWidth = maxHeight = minWidth = minHeight = 0;
                //ѭ����ȡ������ߺ��ϱ���С����
                foreach (PictureBox pb in panelImages.Controls)
                {
                    if (panelImages.Controls.GetChildIndex(pb)==0) 
                    {
                        minWidth = pb.Location.X;
                        minHeight = pb.Location.Y;
                    } 
                    minWidth = Math.Min(minWidth, pb.Location.X);
                    minHeight = Math.Min(minHeight, pb.Location.Y);
                }
                Color bgColor = GetBgColor();
                //������Ԫ�ذ���0��0��Ϊ��׼��ͨ����С���Ͼ�����������ƽ�ƣ���ȡ������
                foreach (PictureBox pb in panelImages.Controls)
                {
                    var point = new Point(pb.Location.X, pb.Location.Y);
                    if (minHeight != 0)
                    {
                        point.Y = pb.Location.Y - minHeight;
                    }
                    if (minWidth != 0)
                    {
                        point.X = pb.Location.X - minWidth;
                    }
                    pb.Location = point;
                    maxWidth = Math.Max(maxWidth, pb.Location.X + pb.Image.Width);
                    maxHeight = Math.Max(maxHeight, pb.Location.Y + pb.Image.Height);
                }
                Size imgSize = new Size(maxWidth, maxHeight);
                var codeMime = string.Empty;
                using (Bitmap bigImg = new Bitmap(imgSize.Width, imgSize.Height, PixelFormat.Format32bppArgb))
                {
                    string imgType = GetImgExt();
                    ImageFormat format = ImageFormat.Png;
                    switch (imgType)
                    {
                        case "jpeg":
                            format = ImageFormat.Jpeg;
                            codeMime = "image/jpeg";
                            break;
                        case "jpg":
                            format = ImageFormat.Jpeg;
                            codeMime = "image/jpeg";
                            break;
                        case "png":
                            format = ImageFormat.Png;
                            codeMime = "image/png";
                            break;
                        case "gif":
                            format = ImageFormat.Gif;
                            codeMime = "image/gif";
                            break;
                        default:
                            break;
                    }
                    using (Graphics g = Graphics.FromImage(bigImg))
                    {
                        //���ø�������ֵ�� 
                        g.InterpolationMode = InterpolationMode.High;
                        //���ø�����,���ٶȳ���ƽ���� 
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        //��ջ�������͸������ɫ��� 
                        g.CompositingQuality = CompositingQuality.HighQuality;
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                        if (bgColor == Color.Transparent && (format == ImageFormat.Jpeg|| format == ImageFormat.Gif)) g.Clear(Color.White);
                        else g.Clear(bgColor);
                        
                        SetCssText();
                        var sprite = new SpriteFile() { CssFileName = txtDir.Text, ImageName = txtName.Text, SpriteList = new List<Sprite>(), IsPhone = chkBoxPhone.Checked };                        
                        try
                        {
                            foreach (PictureBox pb in panelImages.Controls)
                            {
                                var img = (ImageInfo)pb.Image.Tag;
                                var path = img.FileName;
                                Sprite s = new Sprite() { LocationY = pb.Location.Y, LocationX = pb.Location.X, Path = Path.GetFileName(path) };
                                sprite.SpriteList.Add(s);
                                g.DrawImage(pb.Image, pb.Location.X, pb.Location.Y, pb.Image.Width, pb.Image.Height);
                                if (Path.GetDirectoryName(path) != folderBrowserDialog.SelectedPath)
                                {
                                    File.Copy(path, folderBrowserDialog.SelectedPath + "\\" + Path.GetFileName(path), false);
                                }
                            }
                            XmlSerializer.SaveToXml(folderBrowserDialog.SelectedPath + "\\" + txtName.Text + ".sprite", sprite);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message,"��ʾ",MessageBoxButtons.OK,MessageBoxIcon.Error);
                            return;
                        }
                    }
                    try
                    {
                        //����ͼƬ
                        bigImg.Save(imgPath, format);
                        MessageBox.Show("ͼƬ���ɳɹ���");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message+"ͼƬ����ʧ�ܣ��������ļ����ܱ���������ռ�ã��뻻���ļ�����");
                    }
                }
            }
        }


        protected override bool IsInputChar(char charCode)
        {
            if (charCode == (char)Keys.Left || charCode == (char)Keys.Right || charCode == (char)Keys.Up || charCode == (char)Keys.Down)
            {
                return true;
            }

            return base.IsInputChar(charCode);
        }

        

        public bool IsImgExists(string fileName)
        {
            foreach (ImageInfo  ii in _imgList)
            {
                if (string.Compare(ii.FileName,fileName,true) == 0)
                {
                    return true;
                }
            }
            return false;
        }

        


        /// <summary>
        /// ��ȡ��ɫ
        /// </summary>
        /// <returns></returns>
        Color GetBgColor()
        {
            Color bgColor = Color.Transparent;
            string strBgColor = comboBoxBgColor.Text;
            if (!string.IsNullOrEmpty(strBgColor))
            {
                string[] knownColors = Enum.GetNames(typeof(KnownColor));
                bool isKnownColor = false;
                foreach (string kc in knownColors)
                {
                    if (kc == strBgColor)
                    {
                        isKnownColor = true;
                        break;
                    }
                }
                if (isKnownColor)
                    bgColor = Color.FromKnownColor((KnownColor)Enum.Parse(typeof(KnownColor), strBgColor));
                else
                {
                    Regex regex = new Regex("#[0-9abcdef]{6}", RegexOptions.IgnoreCase);
                    if (regex.IsMatch(strBgColor))
                    {
                        int red = int.Parse(strBgColor.Substring(1, 2),NumberStyles.AllowHexSpecifier);
                        int green = int.Parse(strBgColor.Substring(3, 2), NumberStyles.AllowHexSpecifier);
                        int blue = int.Parse(strBgColor.Substring(5, 2), NumberStyles.AllowHexSpecifier);
                        bgColor = Color.FromArgb(red,green,blue);
                    }
                    else {
                        bgColor = Color.Transparent;
                    }
                }
            }
            return bgColor;
        }

        private void comboBoxBgColor_Changed(object sender, EventArgs e)
        {
            Color bgColor = GetBgColor();
            if (bgColor == Color.Transparent)
            {
                bgColor = Color.White;
            }

            panelImages.BackColor = bgColor;
        }

        private void txtSass_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.A && e.Control) { txtSass.SelectAll(); }   
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            var radio = sender as RadioButton;
            if (radio.Checked) {
                txtCss.Visible = false;
                txtSass.Visible = true;
            }
        }

        private void radioBtnCss_CheckedChanged(object sender, EventArgs e)
        {
            var radio = sender as RadioButton;
            if (radio.Checked)
            {
                txtCss.Visible = true;
                txtSass.Visible = false;
            }
        }

        private void txtDir_TextChanged(object sender, EventArgs e)
        {
            SetCssText();
        }

        private void txtName_TextChanged(object sender, EventArgs e)
        {
            SetCssText();
        }

        //Сͼ���ŵ��
        private void buttonHRange_Click(object sender, EventArgs e)
        {
            if (!AssertFiles()) return;
            panelImages.Controls.Clear();
            int left = 0;
            int top = 0;
            foreach (ImageInfo ii in _imgList)
            {
                Image img = ii.Image;
                AddPictureBox(img, left, top);
                left += img.Width;
            }

            panelImages.ResumeLayout(false);
            SetCssText();
        }

        private void chkBoxPhone_CheckedChanged(object sender, EventArgs e)
        {
            SetCssText();
        }

        private void btn_Click(object sender, EventArgs e)
        {
            AboutUs a=new AboutUs();
            a.ShowDialog();
        }
    }
}