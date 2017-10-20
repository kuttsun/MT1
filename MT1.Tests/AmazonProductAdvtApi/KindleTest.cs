using System;

using Microsoft.Extensions.Logging;

using Xunit;

using MT1.AmazonProductAdvtApi.Kindle;

namespace MT1.Tests
{
    public class KindleTest
    {
        ILoggerFactory loggerFactory;
        ILogger<Kindle> logger;

        public KindleTest()
        {
            loggerFactory = new LoggerFactory();
            logger = loggerFactory.CreateLogger<Kindle>();
        }

        [Theory,
            InlineData("����", "�y���Ԍ��薳��&amp;50%OFF�z�u�ā��d���v�����E�����R�~�b�N�Z�[�� �i8/24�܂Łj"),
            InlineData("�|�C���g�Ҍ�", "�y50 % �|�C���g�Ҍ��z�G���^������V�{�i�܂ōu�k�Ђ̃~�X�e���[�t�F�A"),
            InlineData("�L�����y�[��", "�y50%�|�C���g�Ҍ��z�@SB�N���G�C�e�B�u�L�����y�[��"),
            InlineData("���Ԍ���", "�y���Ԍ��薳��������&amp;50%OFF�z�Ă�TL�܂��1�e �R���˂ːV�������L�O�t�F�A �i8/10�܂Łj"),
            InlineData("����", "�y���Ԍ��薳��������&amp;50%OFF�z�Ă�TL�܂��1�e �R���˂ːV�������L�O�t�F�A �i8/10�܂Łj"),
            InlineData("�ψ�", "�y200�~�ψ�z�N�}���K�@1�`5���Z�[���i8/3�܂Łj"),
            InlineData("���W�E�t�F�A", "�y50%OFF�z2017�Ẵr�W�l�X�E���p���t�F�A �i7/27�܂Łj"),
            InlineData("�Z�[��", "Kindle�{�Z�[��"),
            InlineData("���̑�", "�������߂̖{ ")]
        public void ExtractTagsTest(string expected, string title)
        {
            var kindle = new Kindle(logger, null, null);
            Assert.True(kindle.ExtractLabels(title).Contains(expected));
        }

        [Theory,
            InlineData("8/17", "�y���Ԍ��薳��&amp;50%OFF�z�u�ā��d���v���̍��݁`���x���W�`���W (8/17�܂�)"),
            InlineData("6/8", "�y���Ԍ��薳��������&amp;�ʏ�Ŕ��z�z�����E���m�x�̃R�~�J���C�Y��i�t�F�A�i6/8�܂Łj"),
            InlineData("8/14", "�y�ő�50%�|�C���g�Ҍ��z3���Ԍ���S�_�t�F�A�i8/14�܂Łj�i�I��"),
            InlineData(null, "�y50%�|�C���g�Ҍ��z�@SB�N���G�C�e�B�u�L�����y�[��")]
        public void ExtractEndDateTest(string expected, string title)
        {
            var kindle = new Kindle(logger, null, null);
            Assert.Equal(expected, kindle.ExtractEndDate(title));
        }
    }
}
