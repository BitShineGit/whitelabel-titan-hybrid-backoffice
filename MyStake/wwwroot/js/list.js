//function sign() {
//    document.getElementById("myModal").style.display = "block";
//    document.getElementById("MyModal").style.display = "none";
//}
//function up() {
//    document.getElementById("myModal").style.display = "none";
//    document.querySelector(".modal-backdrop").style.display = "none";

//}

function openNav() {
    document.getElementById("mySidenav").style.width = "280px";
    document.getElementById("spacesidenav").style.display = "flex";
    document.getElementById("body").style.overflow = "auto";
}
window.onclick = function (event) {
    if (event.target == document.getElementById("spacesidenav")) {
        document.getElementById("spacesidenav").style.display = "none";
        document.getElementById("mySidenav").style.width = "0px";
    }
    if (event.target == document.getElementById("country-box")) {
        document.getElementById("country").style.transform = "translateY(90%)";
        document.getElementById("country").style.zIndex = "2";
        document.getElementById("country-box").style.visibility = "hidden";
    }
    if (event.target == document.getElementById("currency-box")) {
        document.getElementById("currency").style.transform = "translateY(90%)";
        document.getElementById("currency").style.zIndex = "2";
        document.getElementById("currency-box").style.visibility = "hidden";
    }
};

const Dropdowns = document.querySelectorAll(".country-dropdown");

Dropdowns.forEach((Dropdown) => {
    const selecteD = Dropdown.querySelector(".country-dropdown-selected");

    const menU = Dropdown.querySelector(".country-dropdown-menu");

    const itemS = Dropdown.querySelectorAll(".country-dropdown-item");

    const selectedTexT = Dropdown.querySelector(".selected-texT");

    // OPEN DropDOWN
    selecteD.addEventListener("click", () => {
        // CLOSE OTHER DropDOWNS
        //   Dropdowns.forEach((otherDropdown) => {
        //    if (otherDropdown !== Dropdown) {
        //       otherDropdown
        //         .querySelector(".Dropdown-menu")
        //         .classList.remove("show");
        //
        //       otherDropdown
        //         .querySelector(".Dropdown-selected")
        //         .classList.remove("active");
        //    }
        //   });

        menU.classList.toggle("show");

        selecteD.classList.toggle("active");
    });

    // SELECT ITEM
    itemS.forEach((item) => {
        item.addEventListener("click", () => {
            selectedTexT.textContent = item.dataset.value;

            menU.classList.remove("show");

            selecteD.classList.remove("active");
        });
    });
});

// CLOSE ON OUTSIDE CLICK
document.addEventListener("click", (e) => {
    if (!e.target.closest(".country-dropdown")) {
        Dropdowns.forEach((Dropdown) => {
            Dropdown.querySelector(".country-dropdown-menu").classList.remove("show");

            Dropdown.querySelector(".country-dropdown-selected").classList.remove("active");
        });
    }
});

//const DROPDOWN = document.getElementById("pc-country-dropdown");
//const SELECTEDFlag = document.getElementById("SELECTEDFlag");
//const SELECTEDBtn = document.getElementById("SELECTEDBtn");
//const SELECTEDTEXT = document.getElementById("SELECTEDTEXT");
//SELECTEDBtn.addEventListener("click", () => {
//    DROPDOWN.classList.toggle("active");
//});

//document.querySelectorAll(".OPtion").forEach((OPtion) => {
//    OPtion.addEventListener("click", () => {
//        SELECTEDFlag.src = OPtion.dataset.flag;
//        DROPDOWN.classList.remove("active");
//        SELECTEDTEXT.textContent = OPtion.dataset.value;
//    });
//});

//document.addEventListener("click", (e) => {
//    if (!DROPDOWN.contains(e.target)) {
//        DROPDOWN.classList.remove("active");
//    }
//});

const DROPdowns = document.querySelectorAll(".login-country-dropdown");

DROPdowns.forEach((DROPdown) => {
    const selected = DROPdown.querySelector(".login-country-dropdown-selected");

    const menu = DROPdown.querySelector(".login-country-dropdown-menu");

    const items = DROPdown.querySelectorAll(".login-country-dropdown-item");

    const selectedText = DROPdown.querySelector(".selected-text");

    // OPEN DROPDOWN
    selected.addEventListener("click", () => {
        // CLOSE OTHER DROPDOWNS
        if (window.innerWidth > 980) {
            DROPdowns.forEach((otherDROPdown) => {
                if (otherDROPdown !== DROPdown) {
                    otherDROPdown.querySelector(".login-country-dropdown-menu").classList.remove("show");
                    otherDROPdown
                        .querySelector(".login-country-dropdown-selected")
                        .classList.remove("active");
                }
            });

            menu.classList.toggle("show");

            selected.classList.toggle("active");
        }
        else {
        
                document.getElementById("country").style.transform = "translateY(5%)";
                document.getElementById("country").style.zIndex = "2000";
                document.getElementById("country-box").style.visibility = "visible";
                document.getElementById("country-box").style.display = "flex";
                document.getElementById("country").style.display = "flex";
          
}
    });

    // SELECT ITEM
    items.forEach((item) => {
        item.addEventListener("click", () => {
            selectedText.textContent = item.dataset.value;

            menu.classList.remove("show");

            selected.classList.remove("active");
        });
    });
});

// CLOSE ON OUTSIDE CLICK
document.addEventListener("click", (e) => {
    if (!e.target.closest(".login-country-dropdown")) {
        DROPdowns.forEach((DROPdown) => {
            DROPdown.querySelector(".login-country-dropdown-menu").classList.remove("show");
            DROPdown.querySelector(".login-country-dropdown-selected").classList.remove("active");
            document.getElementById("country").style.transform = "translateY(90%)";
            document.getElementById("country").style.zIndex = "2";
            document.getElementById("country-box").style.visibility = "hidden";
        });
    }
});

const EDROPdowns = document.querySelectorAll(".currency-dropdown");

EDROPdowns.forEach((EDROPdown) => {
    const Eselected = EDROPdown.querySelector(".currency-dropdown-selected");

    const Emenu = EDROPdown.querySelector(".currency-dropdown-menu");

    const Eitems = EDROPdown.querySelectorAll(".currency-dropdown-item");

    const EselectedText = EDROPdown.querySelector(".Eselected-text");

    // OPEN DROPDOWN
    Eselected.addEventListener("click", () => {
        // CLOSE OTHER DROPDOWNS
        if (window.innerWidth > 980) {
            EDROPdowns.forEach((otherEDROPdown) => {
                if (otherEDROPdown !== EDROPdown) {
                    otherEDROPdown.querySelector(".currency-dropdown-menu").classList.remove("show");
                    otherEDROPdown
                        .querySelector(".currency-dropdown-selected")
                        .classList.remove("active");
                }
            });

            Emenu.classList.toggle("show");

            Eselected.classList.toggle("active");
        }
        else {

            document.getElementById("currency").style.transform = "translateY(5%)";
            document.getElementById("currency").style.zIndex = "2000";
            document.getElementById("currency-box").style.visibility = "visible";
            document.getElementById("currency-box").style.display = "flex";
            document.getElementById("currency").style.display = "flex";
        }
    });

    // SELECT ITEM
    Eitems.forEach((Eitem) => {
        Eitem.addEventListener("click", () => {
            EselectedText.textContent = Eitem.dataset.value;

            Emenu.classList.remove("show");

            Eselected.classList.remove("active");
        });
    });
});

// CLOSE ON OUTSIDE CLICK
document.addEventListener("click", (e) => {
    if (!e.target.closest(".currency-dropdown")) {
        EDROPdowns.forEach((EDROPdown) => {
            EDROPdown.querySelector(".currency-dropdown-menu").classList.remove("show");
            EDROPdown.querySelector(".currency-dropdown-selected").classList.remove("active");
            document.getElementById("currency").style.transform = "translateY(90%)";
            document.getElementById("currency").style.zIndex = "2";
            document.getElementById("currency-box").style.visibility = "hidden";
        });
    }
});

//const DROpdown = document.getElementById("flag-dropdown");
//const selectedFlag = document.getElementById("selectedFlag");
//const selectedBtn = document.getElementById("selectedBtn");

//selectedBtn.addEventListener("click", () => {
//    DROpdown.classList.toggle("active");
//});

//document.querySelectorAll(".option").forEach((option) => {
//    option.addEventListener("click", () => {
//        selectedFlag.src = option.dataset.flag;
//        DROpdown.classList.remove("active");
//    });
//});

//document.addEventListener("click", (e) => {
//    if (!DROpdown.contains(e.target)) {
//        DROpdown.classList.remove("active");
//    }
//});

      const slider = document.querySelector(".sliders-wrapper");
      const track = document.querySelector(".sliders");
      const cards = document.querySelectorAll(".cardo");

      let isDragging = false;

      let startX = 0;
      let currentTranslate = 0;
      let prevTranslate = 0;

      function cardSize() {
        return cards[0].offsetWidth + 20; // width + gap
      }

      function maxTranslate() {
        return -(track.scrollWidth - slider.clientWidth);
      }

      function setTranslate(x) {
        track.style.transform = `translateX(${x}px)`;
      }

      /* Elastic effect */
      function applyElastic(x) {
        const maxRight = 0;
        const maxLeft = maxTranslate();

        if (x > maxRight) {
          return x * 0.3;
        }

        if (x < maxLeft) {
          const extra = x - maxLeft;
          return maxLeft + extra * 0.3;
        }

        return x;
      }

/* START */
function startDrag(e) {
    isDragging = true;
    slider.classList.add("dragging");

    startX = e.clientX || e.touches[0].clientX;
}

/* MOVE */
function moveDrag(e) {
    if (!isDragging) return;

    const x = e.clientX || e.touches[0].clientX;
    const delta = x - startX;

    currentTranslate = applyElastic(prevTranslate + delta);

    setTranslate(currentTranslate);
}

/* END (IMPORTANT LOGIC HERE) */
function endDrag() {
    if (!isDragging) return;

    isDragging = false;
    slider.classList.remove("dragging");

    const movedBy = currentTranslate - prevTranslate;

    const size = cardSize();

    // current index
    let index = Math.round(Math.abs(prevTranslate) / size);

    // 🔥 HALF CARD THRESHOLD RULE
    if (Math.abs(movedBy) > size / 2) {
        if (movedBy < 0) {
            index += 1; // next card
        } else {
            index -= 1; // previous card
        }
    }

    // clamp
    const maxIndex = cards.length - Math.floor(slider.clientWidth / size);
    index = Math.max(0, Math.min(index, maxIndex));

    const target = -(index * size);

    currentTranslate = target;
    prevTranslate = target;

    track.style.transition = "transform 0.35s cubic-bezier(.22,.9,.2,1)";
    setTranslate(target);

    setTimeout(() => {
        track.style.transition = "";
    }, 350);
}

/* EVENTS */
slider.addEventListener("mousedown", startDrag);
window.addEventListener("mousemove", moveDrag);
window.addEventListener("mouseup", endDrag);

slider.addEventListener("touchstart", startDrag, { passive: true });
window.addEventListener("touchmove", moveDrag, { passive: true });
window.addEventListener("touchend", endDrag);

/* init */
setTranslate(0);







var flag = 0;
var provSectionHeight = document.getElementById("provContainer").offsetHeight;
window.addEventListener("resize", function () {
    provSectionHeight = document.getElementById("provContainer").offsetHeight;
    if (flag) {
        document.getElementById("two-hiddens").style.height =
            `${provSectionHeight}px`;
    }
});
var SectionHeight = document.getElementById("fitcontainer").offsetHeight;
window.addEventListener("resize", function () {
    SectionHeight = document.getElementById("fitcontainer").offsetHeight;
    if (!flag) {
        document.getElementById("two-imgs").style.height = `${SectionHeight}px`;
    }
});

function fu1() {
    document.getElementById("two-hiddens").style.height =
        `${provSectionHeight}px`;
    console.log(provSectionHeight);
    document.getElementById("two-imgs").style.height = "0px";
    flag = 1;
}
function fu2() {
    document.getElementById("two-hiddens").style.height = "0px";
    document.getElementById("two-imgs").style.height = `${SectionHeight}px`;
    flag = 0;
}

const Modal = document.getElementById("Modal");
const iframe = document.getElementById("ModalIframe");
const closeBtn = document.getElementById("closeModal");

/*
One listener for all current and future buttons
*/

var url = "";


/*
Close only with X button
*/
closeBtn.addEventListener("click", function () {
    Modal.classList.remove("show");

    document.body.style.overflow = "";

    iframe.src = "";
});
$(".SLIDER").slick({
    dots: true,
    arrows: true,
    infinite: true,
    speed: 500,
    slidesToShow: 1,
    autoplay: true,
    autoplaySpeed: 2000,
});
$(".sLIDER").slick({
    dots: true,
    arrows: true,
    infinite: true,
    speed: 500,
    slidesToShow: 1,
    autoplay: true,
    autoplaySpeed: 2000,
});


function no(o) {
    document.getElementById("country").style.transform = "translateY(90%)";
    document.getElementById("country").style.zIndex = "2";
    document.getElementById("country-box").style.visibility = "hidden";
    const countrys = document.getElementsByClassName("country-item");
    const ff = countrys[o - 1].dataset.value;
    document.getElementById("myBtn").innerHTML = `${ff}`;
}
function No(o) {
    document.getElementById("currency").style.transform = "translateY(90%)";
    document.getElementById("currency").style.zIndex = "2";
    document.getElementById("currency-box").style.visibility = "hidden";
    const currencys = document.getElementsByClassName("currency-item");
    document.getElementById("mybtn").innerHTML = `${o}`;
}

