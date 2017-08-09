using System;
using Xunit;

using MT1.AmazonProductAdvtApi;

namespace MT1.Test
{
    public class KindleTest
    {
        [Theory,
            InlineData("����", "�y���Ԍ��薳��&amp;50%OFF�z�u�ā��d���v�����E�����R�~�b�N�Z�[�� �i8/24�܂Łj"),
            InlineData("�|�C���g�Ҍ�", "�y50 % �|�C���g�Ҍ��z�G���^������V�{�i�܂ōu�k�Ђ̃~�X�e���[�t�F�A"),
            InlineData("�L�����y�[��", "�y50%�|�C���g�Ҍ��z�@SB�N���G�C�e�B�u�L�����y�[��"),
            InlineData("���Ԍ���", "�y���Ԍ��薳��������&amp;50%OFF�z�Ă�TL�܂��1�e �R���˂ːV�������L�O�t�F�A �i8/10�܂Łj"),
            InlineData("����", "�y���Ԍ��薳��������&amp;50%OFF�z�Ă�TL�܂��1�e �R���˂ːV�������L�O�t�F�A �i8/10�܂Łj"),
            InlineData("�ψ�", "�y200�~�ψ�z�N�}���K�@1�`5���Z�[���i8/3�܂Łj"),
            InlineData("���W�E�t�F�A", "�y50%OFF�z2017�Ẵr�W�l�X�E���p���t�F�A �i7/27�܂Łj")]
        public void ExtractTagsTest(string expected, string title)
        {
            var kindle = new Kindle();
            Assert.True(kindle.ExtractLabels(title).Contains(expected));
        }
    }
}
