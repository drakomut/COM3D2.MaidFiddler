import PyQt5.uic as uic
import sys
import zerorpc
from PyQt5.QtWidgets import QPushButton, QLabel
import maidfiddler.util.util as util
from maidfiddler.util.translation import tr, tr_str

(ui_class, ui_base) = uic.loadUiType(
    open(util.get_resource_path("templates/connect_dialog.ui")))

class ConnectDialog(ui_class, ui_base):
    def __init__(self, main_window):
        super(ConnectDialog, self).__init__()
        self.setupUi(self)
        self.main_window = main_window
        self.client = None

        self.connect_button.clicked.connect(self.try_connect)
        self.close_button.clicked.connect(self.closeEvent)

    def reload(self):
        self.connect_button.setEnabled(True)
        self.port.setEnabled(True)

        for label in self.findChildren(QLabel):
            label.setText(tr(label))

        for button in self.findChildren(QPushButton):
            button.setText(tr(button))

        self.setWindowTitle(tr_str("connect_dialog.title"))
        self.status_label.setStyleSheet("color: black;")
        self.status_label.setText(tr_str("connect_dialog.status.wait"))
        self.client = None

    def closeEvent(self, evt):
        sys.exit(0)

    def try_connect(self):
        self.connect_button.setEnabled(False)
        self.port.setEnabled(False)
        self.client = zerorpc.Client()
        self.status_label.setStyleSheet("color: orange;")
        self.status_label.setText(tr_str("connect_dialog.status.connect").format(util.GAME_ADDRESS, self.port.value()))
        
        try:
            self.client.connect(f"tcp://{util.GAME_ADDRESS}:{self.port.value()}")
            self.client._zerorpc_ping()
        except Exception as ex:
            self.status_label.setStyleSheet("color: red;")
            self.status_label.setText(tr_str("connect_dialog.status.fail").format(str(ex)))
            self.client.close()
            self.client = None
            self.connect_button.setEnabled(True)
            self.port.setEnabled(True)
            return
        
        self.accept()
