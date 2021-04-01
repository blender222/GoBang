toastr.options = {
  'timeOut': 2000,
  'showDuration': 200,
  'hideDuration': 200,
  'preventDuplicates': true,
  'positionClass': 'toast-top-center',
};

let app = new Vue({
  el: '#app',
  data: {
    scenes: 'hall',
    userName: '',
    faction: 'white',
    connection: new signalR.HubConnectionBuilder().withUrl("/goBangHub").build(),
    pieceArray: (function() {
      let arr = new Array(16);
      for (let i = 0; i <= 15; i++) {
        arr[i] = new Array(16);
      }
      return arr;
    })(),
    userCount: null,
    roomList: [
      {RoomName: '家庭教師', PlayerCount: 1, RoomStatus: '進行中'},
      {RoomName: '學員兼助教', PlayerCount: 2, RoomStatus: '等待中'}
    ],
    newRoomName: '',
  },
  methods: {
    piecePosition(i) {
      return {
        top: `${(Math.floor((i - 1) / 15) + 1) * 40}px`,
        left: `${(i % 15 == 0 ? 15 : i % 15) * 40}px`
      }
    },
    setPiece(e) {
      console.log(`x: ${e.target.x}`, `y: ${e.target.y}`);
      // 發送資料
      this.connection.invoke('SetPiece', e.target.x, e.target.y, this.faction).catch(function(err) {
        return console.error(err.toString());
      });
      // 禁止點擊

    },
    joinHall() {
      if (this.userName === '') {
        toastr.error('請輸入ID');
        return;
      }
      this.connection.invoke('CreateUser', app.userName);
      this.scenes = 'hall';
    },
    createRoom() {
      this.connection.invoke('CreateRoom', app.newRoomName);
    },
    joinRoom() {
      this.connection.invoke('JoinRoom', app.newRoomName);
    }
  },
  created: function() {
    //FIXME: 進入房間再綁定座標
    // this.$nextTick(() => {
    //   // 綁定座標 1,1 ~ 15,15
    //   let arr = document.querySelectorAll('#piece-group .piece');
    //   for (let y = 1; y <= 15; y++) {
    //     for (let x = 1; x <= 15; x++) {
    //       this.pieceArray[y][x] = arr[(y - 1) * 15 + x - 1];
    //       this.pieceArray[y][x].x = x;
    //       this.pieceArray[y][x].y = y;
    //       this.pieceArray[y][x].addEventListener('click', this.setPiece);
    //     }
    //   }
    // });
    this.connection.start()
      .then(function() {})
      .catch(function(err) {
        return console.error(err.toString());
      });
    this.connection.on('UpdateUserCount', function(count) {
      app.userCount = count;
    });
    this.connection.on('UpdateRoomList', function() {
      
    });
    //更新雙方棋子
    // this.connection.on('UpdateBoard', function(x, y, color) {
    //   let piece = app.pieceArray[y][x];
    //   piece.classList.add('active');
    //   piece.classList.replace(app.faction, color);
    //   piece.removeEventListener('click', app.setPiece);
    // });
    // 更新在線人數
  },
});